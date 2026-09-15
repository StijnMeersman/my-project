namespace my_project.Domain;

/// <summary>
/// The manager's decision that a person may log hours against a project (spec 001 §5.1). A pure
/// link with a stamp; it has no state of its own. Removing one never deletes time entries — those
/// hours were really worked, so they stay on the project (§5.4, SC-014).
/// </summary>
public class Assignment
{
    private Assignment()
    {
        // EF Core materialisation.
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ProjectId { get; private set; }

    public Project? Project { get; private set; }

    public Guid PersonId { get; private set; }

    public Person? Person { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    /// <summary>
    /// Uniqueness of (project, person) is enforced by the database index rather than here, so two
    /// managers assigning the same person at the same time cannot both win (FR-010, EC-9).
    /// </summary>
    public static Assignment Create(Guid projectId, Guid personId, DateTimeOffset now) => new()
    {
        ProjectId = projectId,
        PersonId = personId,
        AssignedAt = now,
    };
}
