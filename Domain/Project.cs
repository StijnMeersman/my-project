using my_project.Common;

namespace my_project.Domain;

/// <summary>A funded container for work. Every logged hour belongs to exactly one (spec 001 §5.1).</summary>
public class Project
{
    private Project()
    {
        // EF Core materialisation.
    }

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;

    /// <summary>Persisted alongside <see cref="Name"/> so the per-client unique index can be culture-invariant (EC-7).</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public Guid ClientId { get; private set; }

    public Client? Client { get; private set; }

    /// <summary>
    /// The predefined budget, in whole minutes. Always greater than zero, at creation and after
    /// every edit (FR-005). It may sit below the hours already burned — a budget is a plan, not a
    /// constraint on recorded reality (§5.4).
    /// </summary>
    public int BudgetMinutes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Updated on every edit, so a surprising budget can at least be dated (NFR-007, EC-8).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<Assignment> Assignments { get; private set; } = new List<Assignment>();

    public ICollection<TimeEntry> TimeEntries { get; private set; } = new List<TimeEntry>();

    public Duration Budget => Duration.FromMinutes(BudgetMinutes);

    public static Result<Project> Create(string? name, Guid clientId, string? budget, DateTimeOffset now)
    {
        var validated = Validate(name, clientId, budget);
        if (!validated.IsSuccess)
        {
            return Result<Project>.Fail(validated.Errors);
        }

        var (validName, validBudget) = validated.Value;

        return Result<Project>.Ok(new Project
        {
            Name = validName,
            NormalizedName = TextRules.Normalize(validName),
            ClientId = clientId,
            BudgetMinutes = validBudget.Minutes,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    /// <summary>Changes name, client and budget together (FR-008). Leaves the project untouched if anything is invalid.</summary>
    public Result Update(string? name, Guid clientId, string? budget, DateTimeOffset now)
    {
        var validated = Validate(name, clientId, budget);
        if (!validated.IsSuccess)
        {
            return Result.Fail(validated.Errors);
        }

        var (validName, validBudget) = validated.Value;

        Name = validName;
        NormalizedName = TextRules.Normalize(validName);
        ClientId = clientId;
        BudgetMinutes = validBudget.Minutes;
        UpdatedAt = now;

        return Result.Success();
    }

    private static Result<(string Name, Duration Budget)> Validate(string? name, Guid clientId, string? budget)
    {
        List<ValidationError> errors = [];

        var validName = TextRules.RequireName(name, nameof(Name), "Project name");
        errors.AddRange(validName.Errors);

        if (clientId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(ClientId), "A client is required."));
        }

        var validBudget = Duration.Parse(budget, nameof(BudgetMinutes), "The budget");
        errors.AddRange(validBudget.Errors);

        if (validBudget.IsSuccess && validBudget.Value.Minutes == 0)
        {
            errors.Add(new ValidationError(nameof(BudgetMinutes), "The budget must be greater than zero."));
        }

        return errors.Count > 0
            ? Result<(string, Duration)>.Fail(errors)
            : Result<(string, Duration)>.Ok((validName.Value, validBudget.Value));
    }
}
