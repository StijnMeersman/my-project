using my_project.Common;

namespace my_project.Domain;

/// <summary>
/// The employee's decision that a project belongs on their week (spec 005 §5.1).
/// <para>
/// It exists independently of whether any hours were entered — which is precisely why it must be
/// stored rather than derived from the entries: a row added and not yet filled has no time entries to
/// be derived from (FR-007, SC-007).
/// </para>
/// </summary>
public class WeekRow
{
    private WeekRow()
    {
        // EF Core materialisation.
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid PersonId { get; private set; }

    public Person? Person { get; private set; }

    /// <summary>Always a Monday. Identifies the week, exactly as <see cref="TimesheetWeek"/> does.</summary>
    public DateOnly WeekStartDate { get; private set; }

    public Guid ProjectId { get; private set; }

    public Project? Project { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    /// <summary>
    /// Uniqueness of (person, week, project) is enforced by the database index as well as here, so
    /// two tabs adding the same project at the same moment cannot both win (FR-006, SC-006).
    /// </summary>
    public static Result<WeekRow> Create(
        Guid personId,
        DateOnly weekStartDate,
        Guid projectId,
        DateTimeOffset now)
    {
        List<ValidationError> errors = [];

        if (personId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(PersonId), "A row belongs to a person."));
        }

        if (projectId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(ProjectId), "Pick a project to add."));
        }

        if (!Week.IsStart(weekStartDate))
        {
            errors.Add(new ValidationError(nameof(WeekStartDate), "A week starts on a Monday."));
        }

        return errors.Count > 0
            ? Result<WeekRow>.Fail(errors)
            : Result<WeekRow>.Ok(new WeekRow
            {
                PersonId = personId,
                WeekStartDate = weekStartDate,
                ProjectId = projectId,
                AddedAt = now,
            });
    }
}
