using Backend.Domain.Common;
using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

/// <summary>
/// An append-only record of "who changed what, when". Derives from
/// <see cref="BaseEntity"/> rather than <see cref="AuditableEntity"/> on purpose:
/// an audit trail that can itself be edited or soft-deleted is worthless, so it
/// gets timestamps but no modify/delete surface.
/// </summary>
public class AuditLog : BaseEntity
{
    private AuditLog() { } // EF Core

    public AuditLog(
        string entityName,
        string? entityId,
        AuditAction action,
        string? userId,
        string? userName,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? correlationId = null)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        UserName = userName;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        CorrelationId = correlationId;
    }

    public string EntityName { get; private set; } = default!;

    /// <summary>String rather than Guid so non-entity events (Login, Export, ...) can be logged too.</summary>
    public string? EntityId { get; private set; }

    public AuditAction Action { get; private set; }

    public string? UserId { get; private set; }
    public string? UserName { get; private set; }

    /// <summary>JSON snapshot before the change; null for Created and non-entity actions.</summary>
    public string? OldValues { get; private set; }

    /// <summary>JSON snapshot after the change; null for Deleted and non-entity actions.</summary>
    public string? NewValues { get; private set; }

    public string? IpAddress { get; private set; }

    /// <summary>Ties the entry back to the originating HTTP request (see CorrelationIdMiddleware).</summary>
    public string? CorrelationId { get; private set; }
}
