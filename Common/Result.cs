namespace my_project.Common;

/// <summary>One thing that was wrong with an input, tied to the field that caused it.</summary>
/// <param name="Field">Matches the form field name, so it can be pushed straight into ModelState.</param>
public sealed record ValidationError(string Field, string Message);

/// <summary>
/// The outcome of an operation that can fail because of invalid input. Failure is a value, not an
/// exception — invalid input is an expected outcome of a form submission, not a bug.
/// </summary>
public class Result
{
    private static readonly IReadOnlyList<ValidationError> NoErrors = [];

    protected Result(IReadOnlyList<ValidationError> errors) => Errors = errors;

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool IsSuccess => Errors.Count == 0;

    public static Result Success() => new(NoErrors);

    public static Result Fail(string field, string message) => new([new ValidationError(field, message)]);

    public static Result Fail(IReadOnlyList<ValidationError> errors) => new(errors);
}

/// <summary>A <see cref="Result"/> that carries a value when it succeeds.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base([]) => _value = value;

    private Result(IReadOnlyList<ValidationError> errors) : base(errors) => _value = default;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Ok(T value) => new(value);

    public new static Result<T> Fail(string field, string message) =>
        new([new ValidationError(field, message)]);

    public new static Result<T> Fail(IReadOnlyList<ValidationError> errors) => new(errors);
}
