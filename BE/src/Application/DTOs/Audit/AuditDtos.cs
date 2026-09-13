using Backend.Domain.Enums;
using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Audit;

/// <summary>What callers pass to <c>IAuditService.CreateAuditLogAsync</c>.</summary>
public class CreateAuditLogDto
{
    public required string EntityName { get; init; }
    public string? EntityId { get; init; }
    public required AuditAction Action { get; init; }

    /// <summary>Optional payloads; the service serializes them to JSON so callers don't have to.</summary>
    public object? OldValues { get; init; }
    public object? NewValues { get; init; }
}

public class AuditLogDto
{
    public Guid Id { get; init; }
    public string EntityName { get; init; } = default!;
    public string? EntityId { get; init; }
    public string Action { get; init; } = default!;
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? OldValues { get; init; }
    public string? NewValues { get; init; }
    public string? IpAddress { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime CreatedDate { get; init; }
}

/// <summary>Filters for the audit trail screen; inherits paging/sorting from <see cref="PaginationRequest"/>.</summary>
public class AuditLogQuery : PaginationRequest
{
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public AuditAction? Action { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
