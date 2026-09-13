namespace Backend.Shared.Exceptions;

/// <summary>
/// One or more input/field validation errors occurred. Maps to HTTP 400.
/// Distinct from <c>FluentValidation.ValidationException</c> (thrown by the
/// validation pipeline) - this type is for services/domain code that want to
/// raise validation-style failures manually without a FluentValidation validator.
/// Both are handled by the same middleware branch.
/// </summary>
public class ValidationException : AppException
{
    /// <summary>Property name -> list of error messages for that property.</summary>
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", "ValidationError")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string error)
        : base("One or more validation errors occurred.", "ValidationError")
    {
        Errors = new Dictionary<string, string[]> { [propertyName] = [error] };
    }
}
