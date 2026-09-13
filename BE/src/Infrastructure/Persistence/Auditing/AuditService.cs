using Backend.Application.Common;
using Backend.Application.DTOs.Audit;
using Backend.Application.Interfaces.Services;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Extensions;
using Backend.Shared.Helpers;
using Backend.Shared.Pagination;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Auditing;

/// <summary>
/// EF Core-backed audit trail.
/// <para>
/// Entries are staged on the same DbContext as the business change and are
/// therefore committed (or rolled back) with it - an audit row that survives a
/// failed transaction would be a lie. The caller's
/// <c>IUnitOfWork.SaveChangesAsync</c> is what actually persists them.
/// </para>
/// </summary>
public class AuditService(
    ApplicationDbContext context,
    ICurrentUserService currentUserService) : IAuditService
{
    public async Task CreateAuditLogAsync(CreateAuditLogDto entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var log = new AuditLog(
            entry.EntityName,
            entry.EntityId,
            entry.Action,
            currentUserService.UserId?.ToString(),
            currentUserService.Username,
            entry.OldValues is null ? null : JsonHelper.Serialize(entry.OldValues),
            entry.NewValues is null ? null : JsonHelper.Serialize(entry.NewValues),
            currentUserService.IpAddress);

        await context.AuditLogs.AddAsync(log, cancellationToken);
    }

    public async Task<Result<PaginationResult<AuditLogDto>>> GetAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var logs = context.AuditLogs.AsNoTracking();

        if (query.EntityName.HasValue())
            logs = logs.Where(l => l.EntityName == query.EntityName);

        if (query.EntityId.HasValue())
            logs = logs.Where(l => l.EntityId == query.EntityId);

        if (query.UserId.HasValue())
            logs = logs.Where(l => l.UserId == query.UserId);

        if (query.Action.HasValue)
            logs = logs.Where(l => l.Action == query.Action.Value);

        if (query.FromUtc.HasValue)
            logs = logs.Where(l => l.CreatedDate >= query.FromUtc.Value);

        if (query.ToUtc.HasValue)
            logs = logs.Where(l => l.CreatedDate <= query.ToUtc.Value);

        var totalCount = await logs.CountAsync(cancellationToken);

        // Newest first unless the caller explicitly asked for another ordering -
        // an audit screen is useless in insertion order.
        var sorts = query.GetSortDescriptors();
        logs = sorts.Count > 0
            ? logs.ApplySorting(sorts)
            : logs.OrderByDescending(l => l.CreatedDate);

        var items = await logs
            .ApplyPaging(query)
            .Select(l => new AuditLogDto
            {
                Id = l.Id,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Action = l.Action.ToString(),
                UserId = l.UserId,
                UserName = l.UserName,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                IpAddress = l.IpAddress,
                CorrelationId = l.CorrelationId,
                CreatedDate = l.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<AuditLogDto>>.Success(
            PaginationResult<AuditLogDto>.Create(items, totalCount, query));
    }
}
