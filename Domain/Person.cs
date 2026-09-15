using my_project.Common;

namespace my_project.Domain;

/// <summary>
/// Someone who can be assigned to projects and log hours (spec 001 §5.1). In this slice a Person is
/// a record only — it carries no credentials and cannot sign in until the authentication story
/// exists (spec 001 §10, Q1).
/// </summary>
public class Person
{
    private Person()
    {
        // EF Core materialisation.
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string FullName { get; private set; } = string.Empty;

    public EmailAddress Email { get; private set; } = null!;

    /// <summary>Persisted alongside <see cref="Email"/> so the unique index can be culture-invariant (EC-7).</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    public PersonRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<Assignment> Assignments { get; private set; } = new List<Assignment>();

    public bool IsManager => Role == PersonRole.Manager;

    public static Result<Person> Create(string? fullName, string? email, PersonRole role, DateTimeOffset now)
    {
        List<ValidationError> errors = [];

        var validName = TextRules.RequireName(fullName, nameof(FullName), "Full name");
        errors.AddRange(validName.Errors);

        var validEmail = EmailAddress.Parse(email);
        errors.AddRange(validEmail.Errors);

        if (!Enum.IsDefined(role))
        {
            errors.Add(new ValidationError(nameof(Role), "Pick a role of Employee or Manager."));
        }

        if (errors.Count > 0)
        {
            return Result<Person>.Fail(errors);
        }

        return Result<Person>.Ok(new Person
        {
            FullName = validName.Value,
            Email = validEmail.Value,
            NormalizedEmail = validEmail.Value.Normalized,
            Role = role,
            CreatedAt = now,
        });
    }
}
