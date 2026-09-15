namespace my_project.Domain;

/// <summary>How a project stands against its budget (spec 001 FR-016, SC-017).</summary>
public enum BudgetStatus
{
    WithinBudget,
    ApproachingBudget,
    OverBudget,
}

/// <summary>
/// A project's budget against what has been burned against it. Every figure here is derived from
/// live data on each read and never stored (spec 001 §5.4, NFR-003), so it cannot drift from the
/// entries it summarises.
/// </summary>
/// <param name="BudgetMinutes">Always greater than zero.</param>
/// <param name="BurnedMinutes">The sum of <em>all</em> time entries on the project, whatever state their week is in (FR-014).</param>
public readonly record struct BudgetPosition(int BudgetMinutes, int BurnedMinutes)
{
    /// <summary>The threshold at which a project is called out as approaching its budget (§5.4).</summary>
    public const decimal ApproachingThreshold = 0.90m;

    /// <summary>May be negative: an overrun is a meaningful value and is never clamped (FR-015, EC-13).</summary>
    public int RemainingMinutes => BudgetMinutes - BurnedMinutes;

    public decimal Utilisation => BudgetMinutes == 0 ? 0m : (decimal)BurnedMinutes / BudgetMinutes;

    public BudgetStatus Status => BurnedMinutes > BudgetMinutes
        ? BudgetStatus.OverBudget
        : Utilisation >= ApproachingThreshold
            ? BudgetStatus.ApproachingBudget
            : BudgetStatus.WithinBudget;
}
