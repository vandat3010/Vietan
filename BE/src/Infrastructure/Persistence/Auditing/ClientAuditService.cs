using Backend.Application.Common;
using Backend.Application.DTOs.Audit;
using Backend.Application.Interfaces.Services;
using Backend.Domain.Enums;
using Backend.Shared.Constants;
using Backend.Shared.Results;

namespace Backend.Infrastructure.Persistence.Auditing;

public sealed class ClientAuditService(
    ISystemAuditService systemAudit,
    ICurrentUserService currentUser) : IClientAuditService
{
    public async Task<Result<ClientAuditLogResponse>> CreateAsync(
        CreateClientAuditLogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
            return Result<ClientAuditLogResponse>.Failure("ValidationError", "EventType is required.");

        var eventType = request.EventType.Trim();
        if (BackendOwnedAuditActions.IsBackendOwned(eventType))
        {
            return Result<ClientAuditLogResponse>.Failure(
                "Auth.Forbidden",
                $"Event '{eventType}' must be recorded by the backend, not the client.");
        }

        if (string.IsNullOrWhiteSpace(currentUser.Username) && currentUser.OperatorUserId is null)
            return Result<ClientAuditLogResponse>.Failure("Auth.Unauthorized", "Authentication required.");

        var title = string.IsNullOrWhiteSpace(request.Title) ? eventType : request.Title.Trim();
        var detail = request.Detail?.Trim() ?? string.Empty;
        var category = string.IsNullOrWhiteSpace(request.Category) ? "system" : request.Category.Trim().ToLowerInvariant();

        var meta = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["clientCategory"] = category,
            ["clientEventType"] = eventType,
            ["targetType"] = request.TargetType,
            ["targetId"] = request.TargetId
        };
        if (request.Metadata is not null)
        {
            foreach (var kv in request.Metadata)
                meta[kv.Key] = kv.Value;
        }

        // Identity always from server context — ignore any FE spoof fields.
        await systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.ClientEvent,
            EventType = AuditEventType.ClientEvent,
            Status = AuditStatus.Success,
            Module = "ClientAudit",
            EntityType = request.TargetType,
            EntityId = request.TargetId,
            Description = $"{title}: {detail}".Trim().TrimEnd(':'),
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username,
            AdditionalData = meta
        }, cancellationToken);

        var occurredAt = DateTimeOffset.UtcNow;
        return Result<ClientAuditLogResponse>.Success(new ClientAuditLogResponse
        {
            Id = $"client-{occurredAt.ToUnixTimeMilliseconds()}",
            Category = category,
            EventType = eventType,
            Username = currentUser.Username ?? string.Empty,
            Title = title,
            Detail = detail,
            OccurredAt = occurredAt
        });
    }
}
