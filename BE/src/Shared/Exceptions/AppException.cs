namespace Backend.Shared.Exceptions;

/// <summary>
/// Base type for every exception that is thrown ON PURPOSE, anywhere in the
/// solution (Domain, Application, Infrastructure), to signal an expected
/// failure condition (as opposed to a genuine bug/unexpected runtime error).
/// Lives in <c>Shared</c> - not <c>Application</c> - because Domain entities
/// are also allowed to throw these (e.g. <see cref="Backend.Domain.Common.DomainException"/>
/// derives from <see cref="BusinessException"/>), and Domain cannot depend on Application.
/// <para>
/// The Api layer's global exception middleware catches this hierarchy and maps
/// each subtype to the appropriate HTTP status code - see
/// <c>Backend.Api.Middlewares.ExceptionHandlingMiddleware</c>.
/// </para>
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>Machine-readable code (e.g. "User.NotFound") clients can branch on.</summary>
    public string ErrorCode { get; }

    /// <summary>Optional structured context (field name, entity id, limit exceeded, etc.).</summary>
    public IDictionary<string, object?> AdditionalData { get; }

    protected AppException(string message, string errorCode, IDictionary<string, object?>? additionalData = null)
        : base(message)
    {
        ErrorCode = errorCode;
        AdditionalData = additionalData ?? new Dictionary<string, object?>();
    }

    protected AppException(string message, string errorCode, Exception innerException, IDictionary<string, object?>? additionalData = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        AdditionalData = additionalData ?? new Dictionary<string, object?>();
    }
}
