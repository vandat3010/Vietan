namespace Backend.Shared.Exceptions;

/// <summary>Caller is not authenticated, or credentials are invalid. Maps to HTTP 401.</summary>
public class UnauthorizedException(string message = "Authentication is required to access this resource.", IDictionary<string, object?>? additionalData = null)
    : AppException(message, "Unauthorized", additionalData);
