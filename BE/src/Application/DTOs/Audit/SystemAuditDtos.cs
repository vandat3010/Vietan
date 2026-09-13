using Backend.Domain.Enums;
using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Audit;

/// <summary>BE 3.1a — read model for the audit-log screen. Never exposes secrets.</summary>
public class SystemAuditLogDto
{
    public long Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long? UserId { get; init; }
    public string? UserName { get; init; }
    public string? Description { get; init; }
    public string? Module { get; init; }
    public string? Endpoint { get; init; }
    public string? HttpMethod { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? CorrelationId { get; init; }
    public string? AdditionalData { get; init; }
}

/// <summary>Filters for the audit-log query. Paging/sort inherited from <see cref="PaginationRequest"/>.</summary>
public class SystemAuditLogQuery : PaginationRequest
{
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string? UserName { get; set; }
    public long? UserId { get; set; }
    public AuditEventType? EventType { get; set; }
    public string? Action { get; set; }
    public AuditStatus? Status { get; set; }
    public int? HttpStatusCode { get; set; }
}
