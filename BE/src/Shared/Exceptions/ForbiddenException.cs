namespace Backend.Shared.Exceptions;

/// <summary>Caller is authenticated but lacks permission for this action. Maps to HTTP 403.</summary>
public class ForbiddenException(string message = "You do not have permission to perform this action.", IDictionary<string, object?>? additionalData = null)
    : AppException(message, "Forbidden", additionalData);
