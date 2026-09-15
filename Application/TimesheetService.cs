using Microsoft.EntityFrameworkCore;
using my_project.Common;
using my_project.Data;
using my_project.Domain;

namespace my_project.Application;

/// <summary>
/// Names the field a cell-level failure belongs to. A day save only ever touches one date, so the
/// project alone identifies the cell; the page maps these onto its own form indices.
/// </summary>
public static class TimesheetFields
{
    public static string Hours(Guid projectId) => $"hours:{projectId}";

    public static string Note(Guid projectId) => $"note:{projectId}";
}

/// <summary>
/// The employee's own week: which projects are on it, what is logged against each, and what each one
/// is doing to its budget (spec 005 US-001 – US-005).
/// <para>
/// <b>Every method resolves the person from <see cref="ICurrentUser"/> and none of them accepts one.</b>
/// That is FR-022 and NFR-005: reaching a colleague's week is not refused here so much as unsayable,
/// because there is no parameter to say it with (SC-024).
/// </para>
/// </summary>
public sealed class TimesheetService(
    TimeRegistrationDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    /// <summary>The Monday of the week containing today (FR-002).</summary>
    public DateOnly CurrentWeekStart => Week.StartOf(DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));

    /// <summary>
    /// FR-001 – FR-003, FR-009, FR-018, FR-023. Null when no one is signed in, which is the stubbed
    /// identity's problem and not this screen's (ADR-0004).
    /// <para>
    /// Four queries whatever the week holds: the rows, this week's entries, the visible projects with
    /// their budget position aggregated in the same statement, and the assignments. Never one query
    /// per row (NFR-001).
    /// </para>
    /// </summary>
    public async Task<TimesheetWeekView?> GetWeekAsync(DateOnly? anyDateInWeek, CancellationToken ct = default)
    {
        if (currentUser.PersonId is not { } personId)
        {
            return null;
        }

        var weekStart = Week.StartOf(anyDateInWeek ?? CurrentWeekStart);
        var weekEnd = Week.EndOf(weekStart);
        var days = Week.DaysOf(weekStart);

        var rowProjectIds = await db.WeekRows
            .AsNoTracking()
            .Where(r => r.PersonId == personId && r.WeekStartDate == weekStart)
            .Select(r => r.ProjectId)
            .ToListAsync(ct);

        var entries = await db.TimeEntries
            .AsNoTracking()
            .Where(e => e.PersonId == personId && e.Date >= weekStart && e.Date <= weekEnd)
            .Select(e => new { e.ProjectId, e.Date, e.DurationMinutes, e.Note })
            .ToListAsync(ct);

        // §5.2 promises every entry has a row behind it. Taking the union anyway costs nothing and
        // means a week can never show fewer hours than it holds.
        var visibleProjectIds = rowProjectIds.Union(entries.Select(e => e.ProjectId)).ToList();

        // One query serving two jobs: which rows are still writable (FR-009) and which projects may
        // still be added (FR-005, SC-005).
        var assigned = await db.Assignments
            .AsNoTracking()
            .Where(a => a.PersonId == personId)
            .OrderBy(a => a.Project!.Client!.Name)
            .ThenBy(a => a.Project!.Name)
            .Select(a => new { a.ProjectId, ProjectName = a.Project!.Name, ClientName = a.Project.Client!.Name })
            .ToListAsync(ct);

        var assignedIds = assigned.Select(a => a.ProjectId).ToHashSet();

        var projects = visibleProjectIds.Count == 0
            ? []
            : await db.Projects
                .AsNoTracking()
                .Where(p => visibleProjectIds.Contains(p.Id))
                .OrderBy(p => p.Client!.Name)
                .ThenBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    ClientName = p.Client!.Name,
                    p.BudgetMinutes,

                    // The whole project across every person, derived on this read and never cached —
                    // the same rule and the same number the manager sees (FR-018, NFR-006, SUC-08).
                    BurnedMinutes = p.TimeEntries.Sum(e => (int?)e.DurationMinutes) ?? 0,
                })
                .ToListAsync(ct);

        var entriesByProject = entries.ToLookup(e => e.ProjectId);

        var rows = projects
            .Select(p => new TimesheetRowView(
                p.Id,
                p.Name,
                p.ClientName,
                new BudgetPosition(p.BudgetMinutes, p.BurnedMinutes),
                assignedIds.Contains(p.Id),
                [.. days.Select(day =>
                {
                    var entry = entriesByProject[p.Id].FirstOrDefault(e => e.Date == day);
                    return new TimesheetCellView(day, entry?.DurationMinutes, entry?.Note);
                })]))
            .ToList();

        var addable = assigned
            .Where(a => !rowProjectIds.Contains(a.ProjectId))
            .Select(a => new AddableProject(a.ProjectId, a.ProjectName, a.ClientName))
            .ToList();

        return new TimesheetWeekView(
            personId,
            currentUser.DisplayName,
            weekStart,
            days,
            IsCurrentWeek: weekStart == CurrentWeekStart,

            // Correcting last week is the normal case, so the past stays open; a week that has not
            // begun is read-only (FR-023, SC-003).
            IsEditable: weekStart <= CurrentWeekStart,
            rows,
            addable,
            HasAnyAssignment: assigned.Count > 0);
    }

    /// <summary>FR-005 – FR-007. Adding a row is a decision that outlives an empty week (SC-004, SC-007).</summary>
    public async Task<Result> AddRowAsync(DateOnly weekStart, Guid projectId, CancellationToken ct = default)
    {
        var personId = currentUser.RequirePerson();

        var writable = CheckWritable(weekStart);
        if (!writable.IsSuccess)
        {
            return writable;
        }

        var projectName = await db.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct);

        if (projectName is null)
        {
            return Result.Fail(nameof(WeekRow.ProjectId), "That project no longer exists.");
        }

        // Checked server-side even though the list never offered it (NFR-005, EC-20).
        if (!await db.Assignments.AnyAsync(a => a.PersonId == personId && a.ProjectId == projectId, ct))
        {
            return NotAssigned(projectName);
        }

        if (await db.WeekRows.AnyAsync(
                r => r.PersonId == personId && r.WeekStartDate == weekStart && r.ProjectId == projectId,
                ct))
        {
            return AlreadyOnThisWeek(projectName);
        }

        var created = WeekRow.Create(personId, weekStart, projectId, clock.GetUtcNow());
        if (!created.IsSuccess)
        {
            return created;
        }

        db.WeekRows.Add(created.Value);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (UniqueIndexViolation.Caused(ex))
        {
            // Another tab added the same project between the check above and this write. The index
            // held, so the week still shows it exactly once — say so rather than erroring (SC-006).
            db.Entry(created.Value).State = EntityState.Detached;
            return AlreadyOnThisWeek(projectName);
        }

        return Result.Success();
    }

    /// <summary>
    /// FR-008. A row with hours that week refuses to go: removing it would either strand the hours or
    /// silently delete them, and deleting hours is what clearing a cell is for (SC-009).
    /// </summary>
    public async Task<Result> RemoveRowAsync(DateOnly weekStart, Guid projectId, CancellationToken ct = default)
    {
        var personId = currentUser.RequirePerson();

        var writable = CheckWritable(weekStart);
        if (!writable.IsSuccess)
        {
            return writable;
        }

        var row = await db.WeekRows
            .FirstOrDefaultAsync(
                r => r.PersonId == personId && r.WeekStartDate == weekStart && r.ProjectId == projectId,
                ct);

        if (row is null)
        {
            // Already gone — two tabs removing the same row is not an error worth showing anyone.
            return Result.Success();
        }

        var hasHours = await db.TimeEntries.AnyAsync(
            e => e.PersonId == personId
                && e.ProjectId == projectId
                && e.Date >= weekStart
                && e.Date <= Week.EndOf(weekStart),
            ct);

        if (hasHours)
        {
            var projectName = await db.Projects
                .AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct) ?? "That project";

            return Result.Fail(
                nameof(WeekRow.ProjectId),
                $"\"{projectName}\" has hours saved this week. Clear them and save those days first.");
        }

        db.WeekRows.Remove(row);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    /// <summary>
    /// FR-013 – FR-016. The unit of commit is a whole day column, because the 24-hour rule is a
    /// property of a day and can only be checked with the day in hand (FR-014).
    /// <para>
    /// Every cell is validated before any is written, and the writes go out in one
    /// <c>SaveChangesAsync</c> — so a rejected day leaves every cell exactly as it was, and a failure
    /// mid-save leaves nothing half-applied (NFR-004, SC-015, SUC-05).
    /// </para>
    /// </summary>
    /// <param name="weekStart">The week the grid was showing. A day save never writes outside it (EC-19).</param>
    public async Task<Result<DaySaveResult>> SaveDayAsync(
        DateOnly weekStart,
        DateOnly date,
        IReadOnlyList<DayCellInput> cells,
        CancellationToken ct = default)
    {
        var personId = currentUser.RequirePerson();

        if (!Week.IsWeekday(date))
        {
            // Weekend hours are not hidden, they are unrecordable (FR-004, SC-025).
            return Result<DaySaveResult>.Fail(string.Empty, "Hours can only be logged Monday to Friday.");
        }

        if (!Week.Holds(weekStart, date))
        {
            return Result<DaySaveResult>.Fail(string.Empty, "That day is not part of the week you are looking at.");
        }

        var writable = CheckWritable(weekStart);
        if (!writable.IsSuccess)
        {
            return Result<DaySaveResult>.Fail(writable.Errors);
        }

        var rowProjectIds = await db.WeekRows
            .AsNoTracking()
            .Where(r => r.PersonId == personId && r.WeekStartDate == weekStart)
            .Select(r => r.ProjectId)
            .ToListAsync(ct);

        var assignedIds = (await db.Assignments
                .AsNoTracking()
                .Where(a => a.PersonId == personId && rowProjectIds.Contains(a.ProjectId))
                .Select(a => a.ProjectId)
                .ToListAsync(ct))
            .ToHashSet();

        var projectNames = await db.Projects
            .AsNoTracking()
            .Where(p => rowProjectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var existing = await db.TimeEntries
            .Where(e => e.PersonId == personId && e.Date == date)
            .ToListAsync(ct);

        List<ValidationError> errors = [];
        List<(Guid ProjectId, Duration? Hours, string? Note)> writes = [];
        HashSet<Guid> seen = [];

        foreach (var cell in cells)
        {
            if (!seen.Add(cell.ProjectId))
            {
                continue;
            }

            var hoursField = TimesheetFields.Hours(cell.ProjectId);

            if (!rowProjectIds.Contains(cell.ProjectId))
            {
                // Nothing the grid ever offered, so there is no cell to hang this on when the week
                // is redrawn — it has to be said above the grid instead (EC-20).
                errors.Add(new ValidationError(
                    string.Empty, "A project that is not on this week cannot take hours."));
                continue;
            }

            var hours = TimesheetCell.ParseHours(cell.Hours, hoursField);
            errors.AddRange(hours.Errors);

            var note = TimesheetCell.ParseNote(cell.Note, TimesheetFields.Note(cell.ProjectId));
            errors.AddRange(note.Errors);

            if (!hours.IsSuccess || !note.IsSuccess)
            {
                continue;
            }

            if (!assignedIds.Contains(cell.ProjectId))
            {
                // A row the employee has been unassigned from is read-only, not gone (FR-009). Hours
                // typed into it are refused by name; leaving it alone saves the rest of the day and
                // does not touch what is already there (EC-11).
                //
                // Above the grid rather than on the cell, because the redraw turns that row
                // read-only: the input the employee was typing into is not there any more, and the
                // message already names the project it is about.
                if (hours.Value is not null)
                {
                    errors.Add(new ValidationError(
                        string.Empty,
                        NotAssignedMessage(projectNames.GetValueOrDefault(cell.ProjectId, "That project"))));
                }

                continue;
            }

            // A note cannot outlive the hours it explains: clearing the cell discards it (EC-6).
            writes.Add((cell.ProjectId, hours.Value, hours.Value is null ? null : note.Value));
        }

        var written = writes.Select(w => w.ProjectId).ToHashSet();

        // The day total is the whole day, not just the cells this save touches — a read-only row's
        // hours are still hours worked on that date (FR-014).
        var totalMinutes = writes.Sum(w => w.Hours?.Minutes ?? 0)
            + existing.Where(e => !written.Contains(e.ProjectId)).Sum(e => e.DurationMinutes);

        errors.AddRange(TimesheetCell.CheckDayTotal(date, totalMinutes).Errors);

        if (errors.Count > 0)
        {
            // Nothing has been written yet, so "reject the day whole" costs nothing to honour.
            return Result<DaySaveResult>.Fail(errors);
        }

        var entriesByProject = existing.ToDictionary(e => e.ProjectId);
        var now = clock.GetUtcNow();

        foreach (var (projectId, hours, note) in writes)
        {
            entriesByProject.TryGetValue(projectId, out var entry);

            if (hours is null)
            {
                // A cleared cell deletes its entry rather than storing a zero (FR-016, SC-018, EC-5).
                if (entry is not null)
                {
                    db.TimeEntries.Remove(entry);
                }

                continue;
            }

            var field = TimesheetFields.Hours(projectId);
            Result applied;

            if (entry is null)
            {
                var logged = TimeEntry.Log(personId, projectId, date, hours.Value, note, field, now);
                if (logged.IsSuccess)
                {
                    db.TimeEntries.Add(logged.Value);
                }

                applied = logged;
            }
            else
            {
                // Re-saving a day updates the cell in place rather than appending to it (§5.4).
                applied = entry.Revise(hours.Value, note, field, now);
            }

            if (!applied.IsSuccess)
            {
                // The domain refused something the parsing above let through. Drop every pending
                // change so the day is left exactly as it was (NFR-004).
                db.ChangeTracker.Clear();
                return Result<DaySaveResult>.Fail(applied.Errors);
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (UniqueIndexViolation.Caused(ex))
        {
            db.ChangeTracker.Clear();
            return Result<DaySaveResult>.Fail(
                string.Empty,
                $"{Week.NameOfDay(date)} was saved somewhere else at the same moment. Reload the week and try again.");
        }

        return Result<DaySaveResult>.Ok(
            new DaySaveResult(date, totalMinutes, TimesheetCell.IsLongDay(totalMinutes)));
    }

    private Result CheckWritable(DateOnly weekStart)
    {
        if (!Week.IsStart(weekStart))
        {
            return Result.Fail(string.Empty, "A week starts on a Monday.");
        }

        return weekStart > CurrentWeekStart
            ? Result.Fail(string.Empty, "A week that has not started yet cannot be filled in.")
            : Result.Success();
    }

    private static Result NotAssigned(string projectName) =>
        Result.Fail(nameof(WeekRow.ProjectId), NotAssignedMessage(projectName));

    private static string NotAssignedMessage(string projectName) =>
        $"You are not assigned to \"{projectName}\", so no hours can be logged against it.";

    private static Result AlreadyOnThisWeek(string projectName) =>
        Result.Fail(nameof(WeekRow.ProjectId), $"\"{projectName}\" is already on this week.");
}
