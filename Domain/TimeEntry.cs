namespace my_project.Domain;

/// <summary>
/// Hours worked by one person, on one project, on one day.
/// <para>
/// <b>Owned by spec 005, not by spec 001.</b> Spec 001 §5.1 references TimeEntry without defining
/// it, and depends on exactly three things: it belongs to one project, belongs to one person, and
/// carries an amount. It is modelled here — shaped to spec 005 §5.1 so that slice can build on it
/// — purely so burned hours (spec 001 FR-014) has something real to sum. This slice ships no UI
/// for it and enforces none of spec 005's rules (weekdays only, one entry per cell, 24h per day).
/// </para>
/// </summary>
public class TimeEntry
{
    private TimeEntry()
    {
        // EF Core materialisation.
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid PersonId { get; private set; }

    public Person? Person { get; private set; }

    public Guid ProjectId { get; private set; }

    public Project? Project { get; private set; }

    public DateOnly Date { get; private set; }

    /// <summary>Whole minutes, so summing a project's burn is exact arithmetic (spec 001 NFR-004).</summary>
    public int DurationMinutes { get; private set; }

    public string? Note { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Duration Duration => Duration.FromMinutes(DurationMinutes);

    public static TimeEntry Create(
        Guid personId,
        Guid projectId,
        DateOnly date,
        Duration duration,
        string? note,
        DateTimeOffset now) => new()
        {
            PersonId = personId,
            ProjectId = projectId,
            Date = date,
            DurationMinutes = duration.Minutes,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
}
