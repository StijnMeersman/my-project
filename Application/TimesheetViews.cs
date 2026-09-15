using my_project.Domain;

namespace my_project.Application;

/// <summary>One cell of the grid: what is saved there right now (spec 005 FR-010, FR-012).</summary>
/// <param name="Minutes">Null when the cell is empty. Never zero — a cleared cell has no entry at all.</param>
public sealed record TimesheetCellView(DateOnly Date, int? Minutes, string? Note)
{
    public bool HasHours => Minutes is > 0;

    public bool HasNote => !string.IsNullOrEmpty(Note);

    /// <summary>What goes in the input: hh:mm, or nothing at all.</summary>
    public string Hours => Minutes is { } minutes ? Duration.ToHhMm(minutes) : string.Empty;
}

/// <summary>
/// One project row of the week (spec 005 FR-003, FR-018).
/// </summary>
/// <param name="Budget">
/// The whole project's position across every person — the same figure the manager sees in spec 001
/// FR-013, because there is no per-person budget in the domain for a narrower one to come from.
/// </param>
/// <param name="IsAssigned">
/// False for a project the employee has since been unassigned from. The row stays, and stays
/// readable, but takes no new hours (FR-009, SC-010).
/// </param>
public sealed record TimesheetRowView(
    Guid ProjectId,
    string ProjectName,
    string ClientName,
    BudgetPosition Budget,
    bool IsAssigned,
    IReadOnlyList<TimesheetCellView> Cells)
{
    /// <summary>A row with hours cannot be removed until they are cleared (FR-008, SC-009).</summary>
    public bool HasSavedHours => Cells.Any(c => c.HasHours);

    public bool IsEditable(bool weekIsEditable) => weekIsEditable && IsAssigned;
}

/// <summary>A project the employee may still add to this week (spec 005 FR-005, SC-005).</summary>
public sealed record AddableProject(Guid ProjectId, string ProjectName, string ClientName);

/// <summary>Everything the weekly grid renders (spec 005 US-001).</summary>
/// <param name="IsEditable">
/// False for a week starting after the current one: it can be read, but not filled in (FR-023, SC-003).
/// </param>
/// <param name="HasAnyAssignment">
/// Distinguishes "no projects assigned to you at all" from "none added to this week yet" — two empty
/// states with two different answers (EC-1, EC-2).
/// </param>
public sealed record TimesheetWeekView(
    Guid PersonId,
    string PersonName,
    DateOnly WeekStart,
    IReadOnlyList<DateOnly> Days,
    bool IsCurrentWeek,
    bool IsEditable,
    IReadOnlyList<TimesheetRowView> Rows,
    IReadOnlyList<AddableProject> AddableProjects,
    bool HasAnyAssignment)
{
    public DateOnly PreviousWeek => WeekStart.AddDays(-7);

    public DateOnly NextWeek => WeekStart.AddDays(7);
}

/// <summary>What one cell of a day column was typed as, straight from the form.</summary>
public sealed record DayCellInput(Guid ProjectId, string? Hours, string? Note);

/// <summary>
/// The outcome of a day that saved (spec 005 FR-014, FR-015).
/// </summary>
/// <param name="TotalMinutes">The whole day for this person, including rows the save did not write.</param>
/// <param name="IsLongDay">Over 12:00 — said out loud, never blocked (SC-016).</param>
public sealed record DaySaveResult(DateOnly Date, int TotalMinutes, bool IsLongDay);
