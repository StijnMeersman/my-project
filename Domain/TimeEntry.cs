using my_project.Common;

namespace my_project.Domain;

/// <summary>
/// Hours worked by one person, on one project, on one day (spec 005 §5.1). This is the entity spec
/// 001 §5.1 references but deliberately does not define.
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

    /// <summary>
    /// Records hours the way the weekly grid does, enforcing spec 005 §5.4: weekdays only, never
    /// empty, never more than a day's worth in one cell.
    /// </summary>
    /// <param name="field">
    /// The caller's name for the cell being written, so a rejection lands on the input that caused it.
    /// </param>
    public static Result<TimeEntry> Log(
        Guid personId,
        Guid projectId,
        DateOnly date,
        Duration duration,
        string? note,
        string field,
        DateTimeOffset now)
    {
        var validated = Validate(date, duration, note, field);
        if (!validated.IsSuccess)
        {
            return Result<TimeEntry>.Fail(validated.Errors);
        }

        return Result<TimeEntry>.Ok(new TimeEntry
        {
            PersonId = personId,
            ProjectId = projectId,
            Date = date,
            DurationMinutes = duration.Minutes,
            Note = validated.Value,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    /// <summary>
    /// Re-saving a day updates the cell in place rather than appending a second entry to it (§5.4,
    /// "one entry per cell"). Leaves the entry untouched unless everything validates.
    /// </summary>
    public Result Revise(Duration duration, string? note, string field, DateTimeOffset now)
    {
        var validated = Validate(Date, duration, note, field);
        if (!validated.IsSuccess)
        {
            return Result.Fail(validated.Errors);
        }

        DurationMinutes = duration.Minutes;
        Note = validated.Value;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Materialises an entry without applying spec 005's rules. Reserved for seeds and fixtures that
    /// need arbitrary history to aggregate over — spec 001's burned-hours tests spread entries across
    /// calendar days without caring which of them are weekends. Everything an employee types goes
    /// through <see cref="Log"/>.
    /// </summary>
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

    private static Result<string?> Validate(DateOnly date, Duration duration, string? note, string field)
    {
        List<ValidationError> errors = [];

        // Weekend work is unrecordable by design, not merely unreachable from the grid (FR-004, SC-025).
        if (!Week.IsWeekday(date))
        {
            errors.Add(new ValidationError(field, "Hours can only be logged Monday to Friday."));
        }

        if (duration.Minutes <= 0)
        {
            // Clearing a cell deletes its entry; it never stores a zero (FR-016, EC-5).
            errors.Add(new ValidationError(field, "An entry with no hours is not stored — clear the cell instead."));
        }
        else if (duration.Minutes > TimesheetCell.MaxCellMinutes)
        {
            errors.Add(new ValidationError(field, "A cell cannot exceed 24:00."));
        }

        var validNote = TimesheetCell.ParseNote(note, field);
        errors.AddRange(validNote.Errors);

        return errors.Count > 0
            ? Result<string?>.Fail(errors)
            : Result<string?>.Ok(validNote.Value);
    }
}
