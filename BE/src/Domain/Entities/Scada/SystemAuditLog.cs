using Backend.Domain.Enums;

namespace Backend.Domain.Entities.Scada;

/// <summary>
/// BE 3.1a — System Audit Log (schema <c>scada.system_audit_logs</c>).
/// Answers "who did what, when, from where, on which API, with what result".
/// Append-only by intent; never stores passwords, tokens, secrets or full request
/// bodies. Distinct from the technical application log (Serilog) and from the
/// entity-change trail (<c>app.AuditLog</c>).
/// </summary>
public class SystemAuditLog : ScadaEntity
{
    /// <summary>Specific action, e.g. Login / Logout / Export / ApiError / SystemError.</summary>
    public string Action { get; set; } = string.Empty;

    public AuditEventType EventType { get; set; }

    public AuditStatus Status { get; set; }

    /// <summary>Actor id (SCADA user) when known; never taken from the request body.</summary>
    public long? UserId { get; set; }

    public string? UserName { get; set; }

    /// <summary>Safe, human-readable summary. No secrets, no raw stack traces.</summary>
    public string? Description { get; set; }

    /// <summary>Logical module/feature (e.g. Auth, Users, History).</summary>
    public string? Module { get; set; }

    public string? Endpoint { get; set; }

    public string? HttpMethod { get; set; }

    public int? HttpStatusCode { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    /// <summary>Correlates with the application log / response <c>X-Correlation-Id</c>.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Sanitized JSON metadata (jsonb). Never sensitive data.</summary>
    public string? AdditionalData { get; set; }
}
