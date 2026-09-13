using Backend.Domain.Enums;

namespace Backend.Application.Interfaces.Services;

/// <summary>
/// BE 3.1a — an audit event to record. Context fields (IP, endpoint, method,
/// correlation id, user agent, and actor) are auto-filled from the current
/// <c>HttpContext</c> by the implementation when left null, so callers only supply
/// what they uniquely know (e.g. the login username on a failed auth).
/// </summary>
public sealed class SystemAuditEntry
{
    public required string Action { get; init; }
    public required AuditEventType EventType { get; init; }
    public required AuditStatus Status { get; init; }

    public string? Description { get; init; }
    public string? Module { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public int? HttpStatusCode { get; init; }

    // Explicit actor/context overrides (else enriched from HttpContext).
    public long? UserId { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public string? Endpoint { get; init; }
    public string? HttpMethod { get; init; }
    public string? CorrelationId { get; init; }
    public string? UserAgent { get; init; }

    /// <summary>Optional metadata; sensitive keys are redacted before persistence.</summary>
    public IReadOnlyDictionary<string, object?>? AdditionalData { get; init; }
}

/// <summary>
/// Writes System Audit Log entries. Implementations MUST be fail-safe: a failure to
/// audit is logged to the application log and never propagates to the business flow
/// (BE 3.1a §15). Persists on an isolated DbContext scope so an audit write survives
/// even when the originating request's transaction has failed.
/// </summary>
public interface ISystemAuditService
{
    Task LogAsync(SystemAuditEntry entry, CancellationToken cancellationToken = default);
}
