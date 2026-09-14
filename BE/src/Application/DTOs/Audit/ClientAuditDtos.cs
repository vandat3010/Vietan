using Backend.Domain.Enums;

namespace Backend.Application.DTOs.Audit;

/// <summary>
/// Client-reported audit (UX / secondary). Security-sensitive fields are ignored;
/// actor/IP/timestamp come from the authenticated request context.
/// </summary>
public class CreateClientAuditLogRequest
{
    /// <summary>FE category: login | system (informational only).</summary>
    public string? Category { get; set; }

    /// <summary>Client event type (e.g. session-expired, ui-navigation).</summary>
    public string EventType { get; set; } = string.Empty;

    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }

    /// <summary>Optional metadata — secrets redacted by SystemAuditService.</summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}

public class ClientAuditLogResponse
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

/// <summary>Actions that must be written by backend services — rejected from FE POST.</summary>
public static class BackendOwnedAuditActions
{
    public static readonly HashSet<string> Actions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Login",
        "login",
        "Logout",
        "logout",
        "LoginFailed",
        "login-failed",
        "PasswordChanged",
        "password-changed",
        "AccountLocked",
        "CreateUser",
        "UpdateUser",
        "DeactivateUser",
        "UpdateStation",
        "ConfigurationChanged",
        "Export",
        "AcknowledgeAlarm",
        "ClearAlarm",
        "CreateLicense",
        "UpdateLicense",
        "DeactivateLicense"
    };

    public static bool IsBackendOwned(string? eventType) =>
        !string.IsNullOrWhiteSpace(eventType) && Actions.Contains(eventType.Trim());
}
