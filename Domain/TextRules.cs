using my_project.Common;

namespace my_project.Domain;

/// <summary>
/// The shared rules for the short names used across the domain (spec 001 EC-2, EC-5, EC-7).
/// </summary>
public static class TextRules
{
    public const int MaxNameLength = 200;

    /// <summary>
    /// The form a name is compared in when deciding uniqueness. Culture-invariant on purpose: a
    /// Turkish server must reach the same verdict as a Dutch one (spec 001 EC-7).
    /// </summary>
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    /// <summary>Trims, then rejects an empty or over-long name — never truncates (EC-2, EC-5).</summary>
    public static Result<string> RequireName(string? value, string field, string label)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            return Result<string>.Fail(field, $"{label} is required.");
        }

        return trimmed.Length > MaxNameLength
            ? Result<string>.Fail(field, $"{label} may not be longer than {MaxNameLength} characters.")
            : Result<string>.Ok(trimmed);
    }
}
