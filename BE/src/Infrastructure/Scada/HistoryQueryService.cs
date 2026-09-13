using Backend.Application.DTOs.History;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Pagination;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Scada;

/// <summary>
/// Read-only queries over Timescale hypertables in schema <c>history</c>.
/// Sample queries require a time window so clients cannot accidentally scan the whole table.
/// </summary>
public class HistoryQueryService(ApplicationDbContext db) :
    IHistorySampleQueryService,
    IAlarmHistoryQueryService,
    IScadaEventLogQueryService,
    IUserActivityLogQueryService
{
    public async Task<Result<PaginationResult<HistorySampleDto>>> Get1sAsync(HistorySampleQuery query, CancellationToken cancellationToken = default)
    {
        var validation = ValidateTimeRange(query);
        if (validation is not null) return validation;

        var q = db.History1s.AsNoTracking().Where(h => h.Time >= query.From && h.Time <= query.To);
        q = ApplyTagFilter(q, query);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(h => h.Time)
            .ThenBy(h => h.TagId)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(h => new HistorySampleDto { Time = h.Time, TagId = h.TagId, Value = h.Value })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<HistorySampleDto>>.Success(PaginationResult<HistorySampleDto>.Create(items, total, query));
    }

    public async Task<Result<PaginationResult<HistorySampleDto>>> Get1mAsync(HistorySampleQuery query, CancellationToken cancellationToken = default)
    {
        var validation = ValidateTimeRange(query);
        if (validation is not null) return validation;

        var q = db.History1m.AsNoTracking().Where(h => h.Time >= query.From && h.Time <= query.To);
        q = ApplyTagFilter(q, query);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(h => h.Time)
            .ThenBy(h => h.TagId)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(h => new HistorySampleDto { Time = h.Time, TagId = h.TagId, Value = h.Value })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<HistorySampleDto>>.Success(PaginationResult<HistorySampleDto>.Create(items, total, query));
    }

    public async Task<Result<PaginationResult<HistorySampleDto>>> Get30mAsync(HistorySampleQuery query, CancellationToken cancellationToken = default)
    {
        var validation = ValidateTimeRange(query);
        if (validation is not null) return validation;

        var q = db.History30m.AsNoTracking().Where(h => h.Time >= query.From && h.Time <= query.To);
        q = ApplyTagFilter(q, query);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(h => h.Time)
            .ThenBy(h => h.TagId)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(h => new HistorySampleDto { Time = h.Time, TagId = h.TagId, Value = h.Value })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<HistorySampleDto>>.Success(PaginationResult<HistorySampleDto>.Create(items, total, query));
    }

    private static Result<PaginationResult<HistorySampleDto>>? ValidateTimeRange(HistorySampleQuery query)
    {
        if (query.From is null || query.To is null)
            return Result<PaginationResult<HistorySampleDto>>.Failure(
                "History.TimeRangeRequired",
                "Both 'from' and 'to' query parameters are required for history sample queries.");

        if (query.From > query.To)
            return Result<PaginationResult<HistorySampleDto>>.Failure(
                "History.InvalidTimeRange",
                "'from' must be less than or equal to 'to'.");

        return null;
    }

    private static IQueryable<Domain.Entities.History.History1s> ApplyTagFilter(
        IQueryable<Domain.Entities.History.History1s> q, HistorySampleQuery query)
    {
        if (query.TagIds is { Count: > 0 })
            return q.Where(h => query.TagIds.Contains(h.TagId));
        if (query.TagId is { } tagId)
            return q.Where(h => h.TagId == tagId);
        return q;
    }

    private static IQueryable<Domain.Entities.History.History1m> ApplyTagFilter(
        IQueryable<Domain.Entities.History.History1m> q, HistorySampleQuery query)
    {
        if (query.TagIds is { Count: > 0 })
            return q.Where(h => query.TagIds.Contains(h.TagId));
        if (query.TagId is { } tagId)
            return q.Where(h => h.TagId == tagId);
        return q;
    }

    private static IQueryable<Domain.Entities.History.History30m> ApplyTagFilter(
        IQueryable<Domain.Entities.History.History30m> q, HistorySampleQuery query)
    {
        if (query.TagIds is { Count: > 0 })
            return q.Where(h => query.TagIds.Contains(h.TagId));
        if (query.TagId is { } tagId)
            return q.Where(h => h.TagId == tagId);
        return q;
    }

    public async Task<Result<PaginationResult<AlarmHistoryDto>>> GetPagedAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.AlarmHistories.AsNoTracking().AsQueryable();

        if (query.TagId is { } tagId)
            q = q.Where(a => a.TagId == tagId);
        if (query.DeviceId is { } deviceId)
            q = q.Where(a => a.DeviceId == deviceId);
        if (!string.IsNullOrWhiteSpace(query.Type))
            q = q.Where(a => a.Type == query.Type);
        if (query.IsAcknowledged is { } isAcknowledged)
            q = q.Where(a => a.IsAcknowledged == isAcknowledged);
        if (query.From is { } from)
            q = q.Where(a => a.StartTime >= from);
        if (query.To is { } to)
            q = q.Where(a => a.StartTime <= to);
        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(a =>
                (a.DeviceName != null && a.DeviceName.Contains(keyword)) ||
                (a.TagName != null && a.TagName.Contains(keyword)) ||
                (a.Description != null && a.Description.Contains(keyword)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(a => a.StartTime)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(a => new AlarmHistoryDto
            {
                Id = a.Id,
                StationId = a.StationId,
                PlcId = a.PlcId,
                DeviceId = a.DeviceId,
                TagId = a.TagId,
                DeviceName = a.DeviceName,
                TagName = a.TagName,
                Description = a.Description,
                TroubleshootingGuide = a.TroubleshootingGuide,
                Type = a.Type,
                IsAcknowledged = a.IsAcknowledged,
                DurationSeconds = a.DurationSeconds,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<AlarmHistoryDto>>.Success(PaginationResult<AlarmHistoryDto>.Create(items, total, query));
    }

    async Task<Result<AlarmHistoryDto>> IAlarmHistoryQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.AlarmHistories.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AlarmHistoryDto
            {
                Id = a.Id,
                StationId = a.StationId,
                PlcId = a.PlcId,
                DeviceId = a.DeviceId,
                TagId = a.TagId,
                DeviceName = a.DeviceName,
                TagName = a.TagName,
                Description = a.Description,
                TroubleshootingGuide = a.TroubleshootingGuide,
                Type = a.Type,
                IsAcknowledged = a.IsAcknowledged,
                DurationSeconds = a.DurationSeconds,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                CreatedAt = a.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<AlarmHistoryDto>.Failure("AlarmHistory.NotFound", $"Alarm history '{id}' was not found.")
            : Result<AlarmHistoryDto>.Success(dto);
    }

    public async Task<Result<PaginationResult<EventLogDto>>> GetPagedAsync(EventLogQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.EventLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Level))
            q = q.Where(e => e.Level == query.Level);
        if (!string.IsNullOrWhiteSpace(query.Module))
            q = q.Where(e => e.Module == query.Module);
        if (query.From is { } from)
            q = q.Where(e => e.Time >= from);
        if (query.To is { } to)
            q = q.Where(e => e.Time <= to);
        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(e => e.Message.Contains(keyword) || (e.Exception != null && e.Exception.Contains(keyword)));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(e => e.Time)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(e => new EventLogDto
            {
                Id = e.Id,
                Time = e.Time,
                Level = e.Level,
                Module = e.Module,
                Message = e.Message,
                Exception = e.Exception,
                Machine = e.Machine
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<EventLogDto>>.Success(PaginationResult<EventLogDto>.Create(items, total, query));
    }

    async Task<Result<EventLogDto>> IScadaEventLogQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.EventLogs.AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventLogDto
            {
                Id = e.Id,
                Time = e.Time,
                Level = e.Level,
                Module = e.Module,
                Message = e.Message,
                Exception = e.Exception,
                Machine = e.Machine
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<EventLogDto>.Failure("EventLog.NotFound", $"Event log '{id}' was not found.")
            : Result<EventLogDto>.Success(dto);
    }

    public async Task<Result<PaginationResult<UserActivityLogDto>>> GetPagedAsync(UserActivityLogQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.UserActivityLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Username))
            q = q.Where(u => u.Username == query.Username);
        if (!string.IsNullOrWhiteSpace(query.ActionType))
            q = q.Where(u => u.ActionType == query.ActionType);
        if (!string.IsNullOrWhiteSpace(query.Module))
            q = q.Where(u => u.Module == query.Module);
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(u => u.Status == query.Status);
        if (query.UserId is { } userId)
            q = q.Where(u => u.UserId == userId);
        if (query.From is { } from)
            q = q.Where(u => u.CreatedAt >= from);
        if (query.To is { } to)
            q = q.Where(u => u.CreatedAt <= to);
        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(u => u.Description != null && u.Description.Contains(keyword));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(u => new UserActivityLogDto
            {
                Id = u.Id,
                CreatedAt = u.CreatedAt,
                UserId = u.UserId,
                Username = u.Username,
                Role = u.Role,
                ActionType = u.ActionType,
                Module = u.Module,
                Description = u.Description,
                IpAddress = u.IpAddress,
                Status = u.Status
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<UserActivityLogDto>>.Success(PaginationResult<UserActivityLogDto>.Create(items, total, query));
    }

    async Task<Result<UserActivityLogDto>> IUserActivityLogQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.UserActivityLogs.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserActivityLogDto
            {
                Id = u.Id,
                CreatedAt = u.CreatedAt,
                UserId = u.UserId,
                Username = u.Username,
                Role = u.Role,
                ActionType = u.ActionType,
                Module = u.Module,
                Description = u.Description,
                IpAddress = u.IpAddress,
                Status = u.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<UserActivityLogDto>.Failure("UserActivityLog.NotFound", $"User activity log '{id}' was not found.")
            : Result<UserActivityLogDto>.Success(dto);
    }
}
