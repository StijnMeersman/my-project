using Microsoft.EntityFrameworkCore;
using my_project.Common;
using my_project.Data;
using my_project.Domain;

namespace my_project.Application;

public enum UnassignOutcome
{
    /// <summary>The assignment is gone.</summary>
    Removed,

    /// <summary>The person has logged hours here, so FR-011 wants the manager to see the number first.</summary>
    NeedsConfirmation,

    /// <summary>There was nothing to remove.</summary>
    NotAssigned,
}

/// <param name="LoggedMinutes">What this person logged on this project. Stays on the project either way (SC-014).</param>
public sealed record UnassignResult(UnassignOutcome Outcome, int LoggedMinutes);

/// <summary>Deciding who may log hours against a project (spec 001 US-004).</summary>
public sealed class AssignmentService(
    TimeRegistrationDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    /// <summary>FR-009, FR-010. Assigning someone already assigned reports the fact rather than duplicating them.</summary>
    public async Task<Result> AssignAsync(
        Guid projectId,
        IReadOnlyList<Guid> personIds,
        CancellationToken ct = default)
    {
        currentUser.RequireManager();

        if (personIds.Count == 0)
        {
            return Result.Fail(nameof(Assignment.PersonId), "Pick at least one person to assign.");
        }

        if (!await db.Projects.AnyAsync(p => p.Id == projectId, ct))
        {
            return Result.Fail(string.Empty, "That project no longer exists.");
        }

        var names = await db.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var alreadyAssigned = await db.Assignments
            .Where(a => a.ProjectId == projectId && personIds.Contains(a.PersonId))
            .Select(a => a.PersonId)
            .ToListAsync(ct);

        List<ValidationError> errors = [];
        var now = clock.GetUtcNow();

        foreach (var personId in personIds.Distinct())
        {
            if (!names.TryGetValue(personId, out var name))
            {
                errors.Add(new ValidationError(nameof(Assignment.PersonId), "That person no longer exists."));
                continue;
            }

            if (alreadyAssigned.Contains(personId))
            {
                errors.Add(AlreadyAssigned(name));
                continue;
            }

            db.Assignments.Add(Assignment.Create(projectId, personId, now));
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (UniqueIndexViolation.Caused(ex))
        {
            // Another manager assigned the same person between the check above and this write (EC-9).
            // The index held, so there is still exactly one assignment — say so instead of erroring.
            DetachPendingAssignments();
            errors.Add(new ValidationError(nameof(Assignment.PersonId), "Someone was assigned by another manager at the same moment. Nothing was duplicated."));
        }

        return errors.Count > 0 ? Result.Fail(errors) : Result.Success();
    }

    /// <summary>
    /// FR-011. Removing an assignment stops future logging and nothing else: the hours already
    /// logged were really worked, so they stay on the project and keep counting toward burned
    /// (SC-014). When there are none, there is nothing at stake and no confirmation is asked (EC-12).
    /// </summary>
    public async Task<UnassignResult> UnassignAsync(
        Guid projectId,
        Guid personId,
        bool confirmed,
        CancellationToken ct = default)
    {
        currentUser.RequireManager();

        var assignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.ProjectId == projectId && a.PersonId == personId, ct);

        if (assignment is null)
        {
            return new UnassignResult(UnassignOutcome.NotAssigned, 0);
        }

        var loggedMinutes = await db.TimeEntries
            .Where(e => e.ProjectId == projectId && e.PersonId == personId)
            .Select(e => (int?)e.DurationMinutes)
            .SumAsync(ct) ?? 0;

        if (loggedMinutes > 0 && !confirmed)
        {
            return new UnassignResult(UnassignOutcome.NeedsConfirmation, loggedMinutes);
        }

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(ct);

        return new UnassignResult(UnassignOutcome.Removed, loggedMinutes);
    }

    private void DetachPendingAssignments()
    {
        foreach (var entry in db.ChangeTracker.Entries<Assignment>().Where(e => e.State == EntityState.Added).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static ValidationError AlreadyAssigned(string name) =>
        new(nameof(Assignment.PersonId), $"{name} is already assigned to this project.");
}
