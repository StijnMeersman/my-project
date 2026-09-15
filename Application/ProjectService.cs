using Microsoft.EntityFrameworkCore;
using my_project.Common;
using my_project.Data;
using my_project.Domain;

namespace my_project.Application;

/// <summary>Creating, editing and listing the funded containers hours are logged against (spec 001 US-003, US-005).</summary>
public sealed class ProjectService(
    TimeRegistrationDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    /// <summary>
    /// FR-013 through FR-017. Burned hours is aggregated inside the same query that reads the
    /// projects — one round trip for the whole page, never one per project (NFR-001) — and is
    /// computed from live data on every read rather than cached (NFR-003).
    /// </summary>
    public async Task<IReadOnlyList<ProjectListItem>> ListAsync(CancellationToken ct = default)
    {
        var rows = await db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Client!.Name)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                ClientName = p.Client!.Name,
                p.BudgetMinutes,

                // Every entry counts, whatever state its week is in (FR-014). The nullable cast makes
                // a project with no entries read 0 rather than blank (FR-017, SC-016).
                BurnedMinutes = p.TimeEntries.Sum(e => (int?)e.DurationMinutes) ?? 0,
                AssignedPeopleCount = p.Assignments.Count(),
            })
            .ToListAsync(ct);

        return [.. rows.Select(r => new ProjectListItem(
            r.Id,
            r.Name,
            r.ClientName,
            new BudgetPosition(r.BudgetMinutes, r.BurnedMinutes),
            r.AssignedPeopleCount))];
    }

    public async Task<ProjectDetail?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var header = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ClientId,
                ClientName = p.Client!.Name,
                p.BudgetMinutes,
                BurnedMinutes = p.TimeEntries.Sum(e => (int?)e.DurationMinutes) ?? 0,
            })
            .FirstOrDefaultAsync(ct);

        if (header is null)
        {
            return null;
        }

        var assigned = await db.Assignments
            .AsNoTracking()
            .Where(a => a.ProjectId == id)
            .OrderBy(a => a.Person!.FullName)
            .Select(a => new
            {
                a.PersonId,
                a.Person!.FullName,
                a.Person.Email,
                a.Person.Role,

                // What this person in particular put on this project — the figure the FR-011
                // warning quotes before an assignment is removed (SC-014, EC-12).
                LoggedMinutes = db.TimeEntries
                    .Where(e => e.ProjectId == id && e.PersonId == a.PersonId)
                    .Sum(e => (int?)e.DurationMinutes) ?? 0,
            })
            .ToListAsync(ct);

        var assignedIds = assigned.Select(a => a.PersonId).ToList();
        var assignable = await db.People
            .AsNoTracking()
            .Where(p => !assignedIds.Contains(p.Id))
            .OrderBy(p => p.FullName)
            .ToListAsync(ct);

        return new ProjectDetail(
            header.Id,
            header.Name,
            header.ClientId,
            header.ClientName,
            new BudgetPosition(header.BudgetMinutes, header.BurnedMinutes),
            [.. assigned.Select(a => new AssignedPerson(a.PersonId, a.FullName, a.Email.Value, a.Role, a.LoggedMinutes))],
            assignable);
    }

    public async Task<Project?> FindAsync(Guid id, CancellationToken ct = default) =>
        await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <summary>FR-005, FR-006, FR-007.</summary>
    public async Task<Result<Project>> CreateAsync(
        string? name,
        Guid clientId,
        string? budget,
        CancellationToken ct = default)
    {
        currentUser.RequireManager();

        var created = Project.Create(name, clientId, budget, clock.GetUtcNow());
        if (!created.IsSuccess)
        {
            return created;
        }

        var project = created.Value;

        var clientCheck = await CheckClientExistsAsync(project.ClientId, ct);
        if (!clientCheck.IsSuccess)
        {
            return Result<Project>.Fail(clientCheck.Errors);
        }

        var duplicate = await IsDuplicateAsync(project.ClientId, project.NormalizedName, excludingProjectId: null, ct);
        if (duplicate)
        {
            return Result<Project>.Fail([DuplicateName(project.Name)]);
        }

        db.Projects.Add(project);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (UniqueIndexViolation.Caused(ex))
        {
            db.Entry(project).State = EntityState.Detached;
            return Result<Project>.Fail([DuplicateName(project.Name)]);
        }

        return Result<Project>.Ok(project);
    }

    /// <summary>
    /// FR-008. Name, client and budget change together. Moving a project to a client that already
    /// has a project of that name is refused for the same reason creating one would be (EC-15), and
    /// a budget that drops below what is already burned is accepted — that is the overrun, not an
    /// error (SC-011).
    /// </summary>
    public async Task<Result> UpdateAsync(
        Guid id,
        string? name,
        Guid clientId,
        string? budget,
        CancellationToken ct = default)
    {
        currentUser.RequireManager();

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
        {
            return Result.Fail(string.Empty, "That project no longer exists.");
        }

        var updated = project.Update(name, clientId, budget, clock.GetUtcNow());
        if (!updated.IsSuccess)
        {
            // Nothing was written: Update leaves the entity alone unless every field validated.
            return updated;
        }

        var clientCheck = await CheckClientExistsAsync(project.ClientId, ct);
        if (!clientCheck.IsSuccess)
        {
            db.Entry(project).State = EntityState.Detached;
            return clientCheck;
        }

        if (await IsDuplicateAsync(project.ClientId, project.NormalizedName, excludingProjectId: project.Id, ct))
        {
            db.Entry(project).State = EntityState.Detached;
            return Result.Fail([DuplicateName(project.Name)]);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (UniqueIndexViolation.Caused(ex))
        {
            db.Entry(project).State = EntityState.Detached;
            return Result.Fail([DuplicateName(project.Name)]);
        }

        return Result.Success();
    }

    private async Task<Result> CheckClientExistsAsync(Guid clientId, CancellationToken ct) =>
        await db.Clients.AnyAsync(c => c.Id == clientId, ct)
            ? Result.Success()
            : Result.Fail(nameof(Project.ClientId), "A client is required.");

    private Task<bool> IsDuplicateAsync(
        Guid clientId,
        string normalizedName,
        Guid? excludingProjectId,
        CancellationToken ct) =>
        db.Projects.AnyAsync(
            p => p.ClientId == clientId
                && p.NormalizedName == normalizedName
                && (excludingProjectId == null || p.Id != excludingProjectId),
            ct);

    private static ValidationError DuplicateName(string name) =>
        new(nameof(Project.Name), $"That client already has a project named \"{name}\".");
}
