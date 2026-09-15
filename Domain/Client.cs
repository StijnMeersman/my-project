using my_project.Common;

namespace my_project.Domain;

/// <summary>An organisation work is done for. A project always belongs to exactly one (spec 001 §5.1).</summary>
public class Client
{
    private Client()
    {
        // EF Core materialisation.
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;

    /// <summary>Persisted alongside <see cref="Name"/> so the unique index can be culture-invariant (EC-7).</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<Project> Projects { get; private set; } = new List<Project>();

    public static Result<Client> Create(string? name, DateTimeOffset now)
    {
        var validName = TextRules.RequireName(name, nameof(Name), "Client name");
        if (!validName.IsSuccess)
        {
            return Result<Client>.Fail(validName.Errors);
        }

        return Result<Client>.Ok(new Client
        {
            Name = validName.Value,
            NormalizedName = TextRules.Normalize(validName.Value),
            CreatedAt = now,
        });
    }
}
