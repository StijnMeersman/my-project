namespace my_project.Domain;

/// <summary>
/// Monday-anchored week arithmetic (spec 005 §5.4). A week <em>is</em> its Monday, and it holds five
/// days: weekend work is unrecordable by design (FR-004), so Saturday and Sunday are not part of any
/// week here rather than being part of one and hidden.
/// </summary>
public static class Week
{
    /// <summary>Monday through Friday. Changing this means changing <see cref="IsWeekday"/> too.</summary>
    public const int DayCount = 5;

    /// <summary>The Monday on or before <paramref name="date"/> — the week that date belongs to.</summary>
    public static DateOnly StartOf(DateOnly date) => date.AddDays(-((int)date.DayOfWeek + 6) % 7);

    public static bool IsWeekday(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    public static bool IsStart(DateOnly date) => date.DayOfWeek == DayOfWeek.Monday;

    /// <summary>The five dates of the week beginning <paramref name="weekStart"/>.</summary>
    public static IReadOnlyList<DateOnly> DaysOf(DateOnly weekStart) =>
        [.. Enumerable.Range(0, DayCount).Select(weekStart.AddDays)];

    /// <summary>The last weekday of the week — Friday. Handy as the upper bound of a range query.</summary>
    public static DateOnly EndOf(DateOnly weekStart) => weekStart.AddDays(DayCount - 1);

    /// <summary>True when <paramref name="date"/> is one of the five days the given week can hold.</summary>
    public static bool Holds(DateOnly weekStart, DateOnly date) =>
        IsWeekday(date) && StartOf(date) == weekStart;

    /// <summary>"Monday", "Tuesday", … — the label a column and an error message share.</summary>
    public static string NameOfDay(DateOnly date) => date.DayOfWeek.ToString();
}
