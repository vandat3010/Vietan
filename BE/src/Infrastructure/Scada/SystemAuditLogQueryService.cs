using Backend.Application.DTOs.Audit;
using Backend.Application.Interfaces.Services;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Pagination;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Scada;

/// <summary>BE 3.1a — read side of the System Audit Log. Default sort: newest first.</summary>
public sealed class SystemAuditLogQueryService(ApplicationDbContext db) : ISystemAuditLogQueryService
{
    public async Task<Result<PaginationResult<SystemAuditLogDto>>> GetPagedAsync(
        SystemAuditLogQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.SystemAuditLogs.AsNoTracking().AsQueryable();

        if (query.FromUtc.HasValue)
            q = q.Where(a => a.CreatedAt >= query.FromUtc.Value);

        if (query.ToUtc.HasValue)
            q = q.Where(a => a.CreatedAt <= query.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(query.UserName))
            q = q.Where(a => a.UserName == query.UserName);

        if (query.UserId.HasValue)
            q = q.Where(a => a.UserId == query.UserId.Value);

        if (query.EventType.HasValue)
            q = q.Where(a => a.EventType == query.EventType.Value);

        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(a => a.Action == query.Action);

        if (query.Status.HasValue)
            q = q.Where(a => a.Status == query.Status.Value);

        if (query.HttpStatusCode.HasValue)
            q = q.Where(a => a.HttpStatusCode == query.HttpStatusCode.Value);

        var total = await q.CountAsync(cancellationToken);

        q = ApplyWhitelistedSort(q, query);

        var items = await q
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(a => Project(a))
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<SystemAuditLogDto>>.Success(
            PaginationResult<SystemAuditLogDto>.Create(items, total, query));
    }

    public async Task<Result<SystemAuditLogDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var dto = await db.SystemAuditLogs.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => Project(a))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<SystemAuditLogDto>.Failure("SystemAuditLog.NotFound", $"Audit log '{id}' was not found.")
            : Result<SystemAuditLogDto>.Success(dto);
    }

    // Whitelisted sorting only (§19) — no arbitrary client-supplied order field.
    private static IQueryable<Domain.Entities.Scada.SystemAuditLog> ApplyWhitelistedSort(
        IQueryable<Domain.Entities.Scada.SystemAuditLog> q, SystemAuditLogQuery query)
    {
        var descending = query.SortDirection != SortDirection.Ascending;
        var field = (query.SortBy ?? "timestamp").Trim().ToLowerInvariant();

        return field switch
        {
            "eventtype" => descending ? q.OrderByDescending(a => a.EventType) : q.OrderBy(a => a.EventType),
            "action" => descending ? q.OrderByDescending(a => a.Action) : q.OrderBy(a => a.Action),
            "status" => descending ? q.OrderByDescending(a => a.Status) : q.OrderBy(a => a.Status),
            "username" => descending ? q.OrderByDescending(a => a.UserName) : q.OrderBy(a => a.UserName),
            "httpstatuscode" => descending ? q.OrderByDescending(a => a.HttpStatusCode) : q.OrderBy(a => a.HttpStatusCode),
            // Default and "timestamp"/"createdat": newest first.
            _ => query.SortBy is null ? q.OrderByDescending(a => a.CreatedAt)
                : descending ? q.OrderByDescending(a => a.CreatedAt) : q.OrderBy(a => a.CreatedAt)
        };
    }

    private static SystemAuditLogDto Project(Domain.Entities.Scada.SystemAuditLog a) => new()
    {
        Id = a.Id,
        Timestamp = a.CreatedAt,
        Action = a.Action,
        EventType = a.EventType.ToString(),
        Status = a.Status.ToString(),
        UserId = a.UserId,
        UserName = a.UserName,
        Description = a.Description,
        Module = a.Module,
        Endpoint = a.Endpoint,
        HttpMethod = a.HttpMethod,
        HttpStatusCode = a.HttpStatusCode,
        IpAddress = a.IpAddress,
        UserAgent = a.UserAgent,
        EntityType = a.EntityType,
        EntityId = a.EntityId,
        CorrelationId = a.CorrelationId,
        AdditionalData = a.AdditionalData
    };
}
