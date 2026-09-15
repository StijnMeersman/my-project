using System.Globalization;
using my_project.Common;

namespace my_project.Domain;

/// <summary>
/// What one cell of the weekly grid may hold, and what a day column of them may add up to
/// (spec 005 FR-010 – FR-015, EC-3 – EC-7).
/// <para>
/// Separate from <see cref="Duration.Parse"/> on purpose. That one reads a project budget, where the
/// ceiling is a fat-finger guard and a bare number is the normal way to type one. A cell is a slice of
/// a single day, so it has a real 24-hour ceiling, an empty value that <em>means</em> something
/// (clear the entry), and error messages that name the rule the employee broke rather than the shape
/// of a budget.
/// </para>
/// </summary>
public static class TimesheetCell
{
    /// <summary>A cell is hours within one day, so it can never exceed the day (FR-011, SC-014).</summary>
    public const int MaxCellMinutes = 24 * 60;

    /// <summary>The sum of one person's cells for one date (FR-014, SC-015).</summary>
    public const int MaxDayMinutes = 24 * 60;

    /// <summary>Above this a day is worth mentioning, but never worth refusing (FR-015, SC-016).</summary>
    public const int LongDayMinutes = 12 * 60;

    public const int MaxNoteLength = 500;

    /// <summary>
    /// Reads a typed cell. A blank cell and a cell of <c>0:00</c> both mean <em>no entry</em> — they
    /// return success with no value, because clearing is a legitimate thing to save (FR-016, EC-5) and
    /// a zero-minute entry is never stored (§5.4).
    /// </summary>
    public static Result<Duration?> ParseHours(string? input, string field)
    {
        var text = (input ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return Result<Duration?>.Ok(null);
        }

        var minutes = text.Contains(':') ? ParseHhMm(text, field) : ParseBareHours(text, field);
        if (!minutes.IsSuccess)
        {
            return Result<Duration?>.Fail(minutes.Errors);
        }

        if (minutes.Value > MaxCellMinutes)
        {
            return Result<Duration?>.Fail(field, "A cell cannot exceed 24:00.");
        }

        return Result<Duration?>.Ok(minutes.Value == 0 ? null : Duration.FromMinutes(minutes.Value));
    }

    /// <summary>Trims a note, or rejects one that is too long — never truncates it (FR-012, EC-7).</summary>
    public static Result<string?> ParseNote(string? input, string field)
    {
        var text = (input ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return Result<string?>.Ok(null);
        }

        return text.Length > MaxNoteLength
            ? Result<string?>.Fail(field, $"A note may not be longer than {MaxNoteLength} characters.")
            : Result<string?>.Ok(text);
    }

    /// <summary>
    /// FR-014. The 24-hour rule is a property of a <em>day</em>, not of a cell, so it can only be
    /// checked once the whole column is in hand — which is why a day is the unit of commit.
    /// </summary>
    public static Result CheckDayTotal(DateOnly date, int totalMinutes) =>
        totalMinutes > MaxDayMinutes
            ? Result.Fail(
                string.Empty,
                $"{Week.NameOfDay(date)} totals {Duration.ToHhMm(totalMinutes)} and a day cannot exceed "
                + $"{Duration.ToHhMm(MaxDayMinutes)}. Nothing was saved.")
            : Result.Success();

    /// <summary>FR-015. Worth saying out loud; never worth blocking the save over.</summary>
    public static bool IsLongDay(int totalMinutes) => totalMinutes > LongDayMinutes;

    private static Result<int> ParseHhMm(string text, string field)
    {
        var parts = text.Split(':');

        // NumberStyles.None rejects a sign and any whitespace, so "-1:00" is malformed rather than
        // negative — there is no such thing as negative hours to explain (FR-011).
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var mins))
        {
            return Malformed(field);
        }

        // 1:75 is refused, not normalised to 2:15: the employee meant something, and guessing which
        // is worse than asking (EC-4).
        if (mins > 59)
        {
            return Result<int>.Fail(field, "Minutes must be between 00 and 59.");
        }

        return hours > MaxCellMinutes / 60
            ? Result<int>.Fail(field, "A cell cannot exceed 24:00.")
            : Result<int>.Ok((hours * 60) + mins);
    }

    private static Result<int> ParseBareHours(string text, string field)
    {
        // A bare number is hours: "7" is a full day, not seven minutes (EC-3). InvariantCulture only,
        // so a cell means the same thing whatever the server's locale is.
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var hours))
        {
            return Malformed(field);
        }

        if (hours > MaxCellMinutes / 60)
        {
            return Result<int>.Fail(field, "A cell cannot exceed 24:00.");
        }

        var exactMinutes = hours * 60m;
        return exactMinutes != decimal.Truncate(exactMinutes)
            ? Result<int>.Fail(field, "Hours do not land on a whole minute. Use hh:mm, for example 7:20.")
            : Result<int>.Ok((int)exactMinutes);
    }

    private static Result<int> Malformed(string field) =>
        Result<int>.Fail(field, "Hours must be entered as hh:mm, for example 7:30.");
}
