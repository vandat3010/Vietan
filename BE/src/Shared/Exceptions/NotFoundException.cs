namespace Backend.Shared.Exceptions;

/// <summary>Requested resource does not exist. Maps to HTTP 404.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with identifier '{key}' was not found.", "NotFound",
            new Dictionary<string, object?> { ["entityName"] = entityName, ["key"] = key })
    {
    }

    public NotFoundException(string message, IDictionary<string, object?>? additionalData = null)
        : base(message, "NotFound", additionalData)
    {
    }
}
