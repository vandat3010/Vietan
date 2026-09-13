using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Application.Realtime;
using Backend.Application.Scada;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence.Context;
using Backend.Infrastructure.Realtime;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Scada;

public class ScreenRealtimeQueryService(
    ApplicationDbContext db,
    IRealtimeDataStore store) : IScreenRealtimeQueryService
{
    public async Task<Result<RealtimeSnapshotDto>> GetSnapshotAsync(
        ScadaScreenType screen,
        ScreenTagQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.StationId <= 0)
            return Result<RealtimeSnapshotDto>.Failure("ValidationError", "stationId is required.");

        var stationExists = await db.Stations.AsNoTracking()
            .AnyAsync(s => s.Id == query.StationId, cancellationToken);
        if (!stationExists)
            return Result<RealtimeSnapshotDto>.Failure(ScadaErrorCodes.StationNotFound, ScadaApiMessages.StationNotFoundFor(query.StationId));

        if (screen == ScadaScreenType.ChiTietBom && query.DeviceId is null)
            return Result<RealtimeSnapshotDto>.Failure("ValidationError", "deviceId is required for screen chi-tiet-bom.");

        if (query.DeviceId is { } deviceId)
        {
            var deviceOk = await (
                    from d in db.Devices.AsNoTracking()
                    join p in db.Plcs.AsNoTracking() on d.PlcId equals p.Id
                    where d.Id == deviceId && p.StationId == query.StationId
                    select d.Id)
                .AnyAsync(cancellationToken);
            if (!deviceOk)
                return Result<RealtimeSnapshotDto>.Failure(ScadaErrorCodes.DeviceNotFound, ScadaApiMessages.DeviceNotFound);
        }

        var rows = await LoadMappedTags(screen, query, cancellationToken);
        var values = await store.GetManyAsync(rows.Select(r => r.TagId).ToArray(), cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var tags = new List<RealtimeTagDto>(rows.Count);

        foreach (var row in rows)
        {
            values.TryGetValue(row.TagId, out var live);
            var value = live?.Value;
            var ts = live?.Timestamp ?? now;
            var quality = live?.Quality ?? TagQualityNames.Good;
            if (live is null && ScadaScreenMapping.IsRealtime(screen))
            {
                value = RealtimeValueGenerator.Generate(row.DataType, row.TagId);
                ts = now;
                quality = TagQualityNames.Good;
                await store.SetAsync(row.TagId, new RealtimeValue
                {
                    TagId = row.TagId,
                    Value = value,
                    Timestamp = ts,
                    Quality = quality
                }, cancellationToken);
            }

            tags.Add(new RealtimeTagDto
            {
                TagId = row.TagId,
                DeviceId = row.DeviceId,
                StationId = row.StationId,
                TagCode = row.TagCode,
                TagName = row.TagName,
                DisplayName = row.DisplayName,
                DataType = row.DataType,
                Unit = row.Unit,
                Description = row.Description,
                MappingLabel = row.MappingLabel,
                Value = value,
                Quality = quality,
                Timestamp = ts,
                EnableAlarm = row.EnableAlarm
            });
        }

        return Result<RealtimeSnapshotDto>.Success(new RealtimeSnapshotDto
        {
            StationId = query.StationId,
            DeviceId = query.DeviceId,
            ScreenType = screen,
            Screen = ScadaScreenMapping.ToSlug(screen),
            IsRealtime = ScadaScreenMapping.IsRealtime(screen),
            Tags = tags
        });
    }

    public async Task<Result<PaginationResult<ScreenHistoryPointDto>>> GetHistoryAsync(
        ScadaScreenType screen,
        ScreenHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.StationId <= 0)
            return Result<PaginationResult<ScreenHistoryPointDto>>.Failure("ValidationError", "stationId is required.");

        if (query.From is null || query.To is null)
            return Result<PaginationResult<ScreenHistoryPointDto>>.Failure(
                ScadaErrorCodes.TimeRangeRequired,
                "Both 'from' and 'to' query parameters are required.");

        if (query.From > query.To)
            return Result<PaginationResult<ScreenHistoryPointDto>>.Failure(
                ScadaErrorCodes.InvalidTimeRange,
                "'from' must be less than or equal to 'to'.");

        var mapped = await LoadMappedTags(screen, new ScreenTagQuery { StationId = query.StationId, DeviceId = query.DeviceId }, cancellationToken);
        var tagIds = mapped.Select(m => m.TagId).ToList();
        if (tagIds.Count == 0)
            return Result<PaginationResult<ScreenHistoryPointDto>>.Success(
                PaginationResult<ScreenHistoryPointDto>.Create([], 0, query.PageNumber, query.PageSize));

        var labelByTag = mapped.ToDictionary(m => m.TagId, m => m);
        var interval = (query.Interval ?? "30s").Trim().ToLowerInvariant();
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 100 : Math.Min(query.PageSize, 1000);
        var skip = (pageNumber - 1) * pageSize;

        List<ScreenHistoryPointDto> page;
        int total;

        switch (interval)
        {
            case "1s":
                {
                    var q = db.History1s.AsNoTracking()
                        .Where(h => h.Time >= query.From && h.Time <= query.To && tagIds.Contains(h.TagId));
                    total = await q.CountAsync(cancellationToken);
                    var rows = await q.OrderByDescending(h => h.Time).ThenBy(h => h.TagId)
                        .Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                    page = rows.Select(h => ToPoint(h.Time, h.TagId, h.Value, labelByTag)).ToList();
                    break;
                }
            case "1m":
                {
                    var q = db.History1m.AsNoTracking()
                        .Where(h => h.Time >= query.From && h.Time <= query.To && tagIds.Contains(h.TagId));
                    total = await q.CountAsync(cancellationToken);
                    var rows = await q.OrderByDescending(h => h.Time).ThenBy(h => h.TagId)
                        .Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                    page = rows.Select(h => ToPoint(h.Time, h.TagId, h.Value, labelByTag)).ToList();
                    break;
                }
            case "30m":
                {
                    var q = db.History30m.AsNoTracking()
                        .Where(h => h.Time >= query.From && h.Time <= query.To && tagIds.Contains(h.TagId));
                    total = await q.CountAsync(cancellationToken);
                    var rows = await q.OrderByDescending(h => h.Time).ThenBy(h => h.TagId)
                        .Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                    page = rows.Select(h => ToPoint(h.Time, h.TagId, h.Value, labelByTag)).ToList();
                    break;
                }
            default:
                {
                    var q = db.History30s.AsNoTracking()
                        .Where(h => h.Time >= query.From && h.Time <= query.To && tagIds.Contains(h.TagId));
                    total = await q.CountAsync(cancellationToken);
                    var rows = await q.OrderByDescending(h => h.Time).ThenBy(h => h.TagId)
                        .Skip(skip).Take(pageSize).ToListAsync(cancellationToken);
                    page = rows.Select(h => ToPoint(h.Time, h.TagId, h.Value, labelByTag)).ToList();
                    break;
                }
        }

        return Result<PaginationResult<ScreenHistoryPointDto>>.Success(
            PaginationResult<ScreenHistoryPointDto>.Create(page, total, pageNumber, pageSize));
    }

    private async Task<List<MappedTagRow>> LoadMappedTags(
        ScadaScreenType screen,
        ScreenTagQuery query,
        CancellationToken cancellationToken)
    {
        var deviceId = query.DeviceId;
        var q =
            from m in db.TagScreenMappings.AsNoTracking()
            join t in db.Tags.AsNoTracking() on m.TagId equals t.Id
            join d in db.Devices.AsNoTracking() on t.DeviceId equals d.Id
            join p in db.Plcs.AsNoTracking() on t.PlcId equals p.Id
            where m.ScreenType == screen
                  && p.StationId == query.StationId
                  && (deviceId == null || d.Id == deviceId)
            orderby d.Id, t.Code
            select new MappedTagRow(
                t.Id, d.Id, p.StationId, t.Code, t.TagName, t.DisplayName,
                t.DataType, t.Unit, t.Description, m.MappingLabel, t.EnableAlarm);

        return await q.ToListAsync(cancellationToken);
    }

    private static ScreenHistoryPointDto ToPoint(
        DateTimeOffset time, long tagId, double value, IReadOnlyDictionary<long, MappedTagRow> map)
    {
        map.TryGetValue(tagId, out var meta);
        return new ScreenHistoryPointDto
        {
            Time = time,
            TagId = tagId,
            TagCode = meta?.TagCode ?? string.Empty,
            MappingLabel = meta?.MappingLabel,
            Value = value
        };
    }

    private sealed record MappedTagRow(
        long TagId,
        long DeviceId,
        long StationId,
        string TagCode,
        string TagName,
        string DisplayName,
        string DataType,
        string? Unit,
        string? Description,
        string? MappingLabel,
        bool EnableAlarm);
}
