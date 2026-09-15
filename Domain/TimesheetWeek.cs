namespace my_project.Domain;

/// <summary>The state one employee's week is in (spec 010 §5.1).</summary>
public enum TimesheetWeekStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
}

/// <summary>
/// One employee's week and the state it is in.
/// <para>
/// <b>Owned by spec 010, not by spec 001.</b> Only the identity and the status are modelled here —
/// submission, decision and nudge fields belong to that slice. It exists in this one because spec
/// 001 FR-014 makes a claim *about* status: burned hours count draft, submitted, approved and
/// rejected time alike, and SC-018 is untestable without somewhere for a status to live. A week
/// with no record is Draft (spec 010 §5.4), which is why nothing in this slice creates one.
/// </para>
/// </summary>
public class TimesheetWeek
{
    private TimesheetWeek()
    {
        // EF Core materialisation.
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid PersonId { get; private set; }

    public Person? Person { get; private set; }

    /// <summary>Always a Monday. Identifies the week, exactly as spec 005's WeekRow does.</summary>
    public DateOnly WeekStartDate { get; private set; }

    public TimesheetWeekStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static TimesheetWeek Create(
        Guid personId,
        DateOnly weekStartDate,
        TimesheetWeekStatus status,
        DateTimeOffset now) => new()
        {
            PersonId = personId,
            WeekStartDate = weekStartDate,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now,
        };
}
