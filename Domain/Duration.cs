using System.Globalization;
using my_project.Common;

namespace my_project.Domain;

/// <summary>
/// A length of time held as a whole number of minutes (spec 005 §5.3, which replaces spec 001's
/// 2-decimal <c>Hours</c>). All arithmetic is integer arithmetic, so summing is exact by
/// construction and no rounding rule is ever needed — this is how spec 001 NFR-004 is satisfied.
/// </summary>
public readonly record struct Duration : IComparable<Duration>
{
    /// <summary>100 000 hours — the fat-finger guard from spec 001 EC-4, not a business limit.</summary>
    public const int MaxMinutes = 100_000 * 60;

    private Duration(int minutes) => Minutes = minutes;

    public int Minutes { get; }

    public static Duration Zero => new(0);

    public static Duration FromMinutes(int minutes) => minutes < 0
        ? throw new ArgumentOutOfRangeException(nameof(minutes), "A duration is never negative.")
        : new Duration(minutes);

    /// <summary>
    /// Parses either <c>hh:mm</c> ("120:30") or a number of hours ("120", "7.5"). A value that does
    /// not land on a whole minute is rejected rather than rounded (spec 001 EC-3).
    /// </summary>
    public static Result<Duration> Parse(string? input, string field, string label)
    {
        var text = (input ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return Result<Duration>.Fail(field, $"{label} is required.");
        }

        var minutes = text.Contains(':') ? ParseHhMm(text, field, label) : ParseHours(text, field, label);
        if (!minutes.IsSuccess)
        {
            return Result<Duration>.Fail(minutes.Errors);
        }

        return minutes.Value > MaxMinutes
            ? Result<Duration>.Fail(field, $"{label} may not exceed {MaxMinutes / 60:N0} hours.")
            : Result<Duration>.Ok(new Duration(minutes.Value));
    }

    private static Result<int> ParseHhMm(string text, string field, string label)
    {
        var parts = text.Split(':');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var mins))
        {
            return Result<int>.Fail(field, $"{label} must look like 120:30 or 120.");
        }

        if (mins > 59)
        {
            return Result<int>.Fail(field, $"{label} has minutes outside 00–59.");
        }

        return hours > MaxMinutes / 60
            ? Result<int>.Fail(field, $"{label} may not exceed {MaxMinutes / 60:N0} hours.")
            : Result<int>.Ok((hours * 60) + mins);
    }

    private static Result<int> ParseHours(string text, string field, string label)
    {
        // InvariantCulture only, so a budget means the same thing whatever the server's locale is.
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var hours))
        {
            return Result<int>.Fail(field, $"{label} must be a number of hours, for example 120 or 120:30.");
        }

        if (hours < 0)
        {
            return Result<int>.Fail(field, $"{label} cannot be negative.");
        }

        if (hours > MaxMinutes / 60)
        {
            return Result<int>.Fail(field, $"{label} may not exceed {MaxMinutes / 60:N0} hours.");
        }

        var exactMinutes = hours * 60m;
        return exactMinutes != decimal.Truncate(exactMinutes)
            ? Result<int>.Fail(field, $"{label} does not land on a whole minute. Use hh:mm, for example 120:20.")
            : Result<int>.Ok((int)exactMinutes);
    }

    /// <summary>Renders a signed minute count as hh:mm — used for remaining budget, which may be negative.</summary>
    public static string ToHhMm(int minutes)
    {
        var sign = minutes < 0 ? "-" : string.Empty;
        var abs = Math.Abs(minutes);
        return string.Create(CultureInfo.InvariantCulture, $"{sign}{abs / 60}:{abs % 60:00}");
    }

    public override string ToString() => ToHhMm(Minutes);

    public int CompareTo(Duration other) => Minutes.CompareTo(other.Minutes);

    public static bool operator <(Duration left, Duration right) => left.Minutes < right.Minutes;

    public static bool operator >(Duration left, Duration right) => left.Minutes > right.Minutes;

    public static bool operator <=(Duration left, Duration right) => left.Minutes <= right.Minutes;

    public static bool operator >=(Duration left, Duration right) => left.Minutes >= right.Minutes;
}
