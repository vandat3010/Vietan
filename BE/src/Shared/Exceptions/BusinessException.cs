namespace Backend.Shared.Exceptions;

/// <summary>
/// A business/domain rule was violated (e.g. "cannot delete a system role").
/// Maps to HTTP 400 by default. Prefer a more specific exception
/// (<see cref="NotFoundException"/>, <see cref="ValidationException"/>, ...) when one fits better.
/// </summary>
public class BusinessException(string message, string errorCode = "BusinessRuleViolation", IDictionary<string, object?>? additionalData = null)
    : AppException(message, errorCode, additionalData);
