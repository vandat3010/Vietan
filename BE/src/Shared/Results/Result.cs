namespace Backend.Shared.Results;

/// <summary>
/// Represents the outcome of an operation without a return payload.
/// Used across the Application layer so EXPECTED business failures (validation,
/// not-found, conflict, etc.) can be communicated to controllers without
/// throwing exceptions for control flow. Reserve
/// <see cref="Backend.Shared.Exceptions"/> for truly exceptional/unexpected
/// situations, or for failures raised deep inside the Domain layer where
/// returning a Result all the way up the call stack isn't practical.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string[] Errors { get; }
    public string ErrorCode { get; }

    /// <summary>Populated only for validation-style failures: property name -> messages.</summary>
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(bool isSuccess, string errorCode, string[] errors, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        Errors = errors;
        ValidationErrors = validationErrors;
    }

    public static Result Success() => new(true, string.Empty, []);

    public static Result Failure(string errorCode, params string[] errors) =>
        new(false, errorCode, errors.Length > 0 ? errors : [errorCode]);

    public static Result Failure(string errorCode, IEnumerable<string> errors) =>
        new(false, errorCode, errors.ToArray());

    /// <summary>Convenience factory for field-level validation failures.</summary>
    public static Result ValidationFailure(IDictionary<string, string[]> validationErrors) =>
        new(false, "ValidationError", validationErrors.SelectMany(kv => kv.Value).ToArray(), validationErrors);
}

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/>.
/// </summary>
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, string.Empty, [])
    {
        Value = value;
    }

    private Result(string errorCode, string[] errors, IDictionary<string, string[]>? validationErrors = null)
        : base(false, errorCode, errors, validationErrors)
    {
        Value = default;
    }

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(string errorCode, params string[] errors) =>
        new(errorCode, errors.Length > 0 ? errors : [errorCode]);

    public static new Result<T> Failure(string errorCode, IEnumerable<string> errors) =>
        new(errorCode, errors.ToArray());

    public static new Result<T> ValidationFailure(IDictionary<string, string[]> validationErrors) =>
        new("ValidationError", validationErrors.SelectMany(kv => kv.Value).ToArray(), validationErrors);

    public static implicit operator Result<T>(T value) => Success(value);
}
