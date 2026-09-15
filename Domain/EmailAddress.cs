using System.Text.RegularExpressions;
using my_project.Common;

namespace my_project.Domain;

/// <summary>
/// The address that identifies a person (spec 001 §5.3). Stored as entered for display, compared
/// case-insensitively for uniqueness.
/// </summary>
public sealed partial record EmailAddress
{
    public const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    /// <summary>The form uniqueness is decided in — culture-invariant, per spec 001 EC-7.</summary>
    public string Normalized => TextRules.Normalize(Value);

    /// <summary>A local part, an <c>@</c> and a domain. Deliberately no stricter (spec 001 EC-6).</summary>
    public static Result<EmailAddress> Parse(string? input, string field = "Email", string label = "Email address")
    {
        var trimmed = (input ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            return Result<EmailAddress>.Fail(field, $"{label} is required.");
        }

        if (trimmed.Length > MaxLength)
        {
            return Result<EmailAddress>.Fail(field, $"{label} may not be longer than {MaxLength} characters.");
        }

        return Shape().IsMatch(trimmed)
            ? Result<EmailAddress>.Ok(new EmailAddress(trimmed))
            : Result<EmailAddress>.Fail(field, $"{label} must look like name@example.com.");
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+$")]
    private static partial Regex Shape();
}
