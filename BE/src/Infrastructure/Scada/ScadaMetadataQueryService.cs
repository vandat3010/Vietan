using System.Globalization;
using Backend.Application.Common;
using Backend.Application.Configuration;
using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Application.Options;
using Backend.Application.Realtime;
using Backend.Domain.Entities.Scada;
using Backend.Domain.Enums;
using Backend.Domain.Interfaces;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Constants;
using Backend.Shared.Models;
using Backend.Shared.Pagination;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Scada;

/// <summary>
/// EF read/write-side for SCADA metadata (schema <c>public</c> / <c>app</c>).
/// Lỗi nghiệp vụ và exception DB đều trả <see cref="Result{T}"/> — không throw lên controller.
/// </summary>
public class ScadaMetadataQueryService(
    ApplicationDbContext db,
    IRealtimeDataStore realtimeStore,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUser,
    ISystemAuditService systemAudit,
    IImportExportService importExport,
    IOptions<PasswordPolicyOptions> passwordOptions,
    ILogger<ScadaMetadataQueryService> logger) :
    IStationQueryService,
    IPlcQueryService,
    IDeviceQueryService,
    ITagQueryService,
    IHistoryProfileQueryService,
    ITagHistoryConfigQueryService,
    ICommunicationConfigQueryService,
    IMqttConfigQueryService,
    IScadaUserQueryService,
    IAppSettingQueryService
{
    /// <summary>Bắt exception → Result.Failure (giữ OperationCanceledException).</summary>
    private async Task<Result<T>> SafeAsync<T>(Func<Task<Result<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SCADA query failed");
            return Result<T>.Failure(
                ScadaErrorCodes.Unexpected,
                ScadaApiMessages.UnexpectedError);
        }
    }

    private async Task<Result> SafeAsync(Func<Task<Result>> action)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SCADA command failed");
            return Result.Failure(ScadaErrorCodes.Unexpected, ScadaApiMessages.UnexpectedError);
        }
    }

    private const int MaxExportRows = 10_000;

    public Task<Result<PaginationResult<StationDto>>> GetPagedAsync(StationQuery query, CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
    {
        var q = db.Stations.AsNoTracking().AsQueryable();

        if (query.IsActive is { } isActive)
            q = q.Where(s => s.IsActive == isActive);

        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(s => s.Code.Contains(keyword) || s.Name.Contains(keyword));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(s => s.Code)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(s => new StationDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Address = s.Address,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<StationDto>>.Success(PaginationResult<StationDto>.Create(items, total, query));
        });

    async Task<Result<StationDetailDto>> IStationQueryService.GetByIdAsync(long id, CancellationToken cancellationToken) =>
        await SafeAsync(async () =>
        {
        var dto = await db.Stations.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StationDetailDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Description = s.Description,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<StationDetailDto>.Failure(ScadaErrorCodes.StationNotFound, ScadaApiMessages.StationNotFoundFor(id))
            : Result<StationDetailDto>.Success(dto);
        });

    public Task<Result<StationDetailDto>> UpdateAsync(
        long id,
        UpdateStationRequest request,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var station = await db.Stations.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (station is null)
                return Result<StationDetailDto>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(id));

            if (request.Name is not null)
            {
                var name = request.Name.Trim();
                if (name.Length == 0)
                    return Result<StationDetailDto>.Failure("ValidationError", "Station name cannot be empty.");
                if (name.Length > 200)
                    return Result<StationDetailDto>.Failure("ValidationError", "Station name is too long.");
                station.Name = name;
            }

            if (request.Address is not null)
                station.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
            if (request.Description is not null)
                station.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            if (request.Latitude is { } lat)
            {
                if (lat is < -90 or > 90)
                    return Result<StationDetailDto>.Failure("ValidationError", "Latitude must be between -90 and 90.");
                station.Latitude = lat;
            }
            if (request.Longitude is { } lon)
            {
                if (lon is < -180 or > 180)
                    return Result<StationDetailDto>.Failure("ValidationError", "Longitude must be between -180 and 180.");
                station.Longitude = lon;
            }
            if (request.IsActive is { } isActive)
                station.IsActive = isActive;

            station.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await systemAudit.LogAsync(new SystemAuditEntry
            {
                Action = AuditActionNames.UpdateStation,
                EventType = AuditEventType.Configuration,
                Status = AuditStatus.Success,
                Module = "Stations",
                EntityType = "Station",
                EntityId = station.Id.ToString(CultureInfo.InvariantCulture),
                Description = $"Updated station '{station.Code}'.",
                UserId = currentUser.OperatorUserId,
                UserName = currentUser.Username
            }, cancellationToken);

            return Result<StationDetailDto>.Success(new StationDetailDto
            {
                Id = station.Id,
                Code = station.Code,
                Name = station.Name,
                Address = station.Address,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                Description = station.Description,
                IsActive = station.IsActive
            });
        });

    public Task<Result<PaginationResult<ActiveAlarmRowDto>>> GetActiveAlarmsAsync(
        long stationId,
        ActiveAlarmQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<PaginationResult<ActiveAlarmRowDto>>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            var stationDeviceIds = await db.Devices.AsNoTracking()
                .Where(d => d.Plc.StationId == stationId)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            var q = db.AlarmHistories.AsNoTracking()
                .Where(a => a.EndTime == null)
                .Where(a =>
                    a.StationId == stationId
                    || (a.DeviceId != null && stationDeviceIds.Contains(a.DeviceId.Value)));

            if (query.DeviceId is { } deviceId && deviceId > 0)
                q = q.Where(a => a.DeviceId == deviceId);
            if (query.IsAcknowledged is { } ack)
                q = q.Where(a => a.IsAcknowledged == ack);
            if (!string.IsNullOrWhiteSpace(query.Type))
                q = q.Where(a => a.Type == query.Type);
            if (query.HasKeyword)
            {
                var keyword = query.Keyword!;
                q = q.Where(a =>
                    (a.DeviceName != null && a.DeviceName.Contains(keyword)) ||
                    (a.TagName != null && a.TagName.Contains(keyword)) ||
                    (a.Description != null && a.Description.Contains(keyword)) ||
                    (a.Type != null && a.Type.Contains(keyword)));
            }

            var total = await q.CountAsync(cancellationToken);
            var raw = await q
                .OrderByDescending(a => a.StartTime)
                .ThenByDescending(a => a.Id)
                .Skip(query.Skip)
                .Take(query.PageSize)
                .Select(a => new
                {
                    a.Id,
                    a.StartTime,
                    a.EndTime,
                    a.Type,
                    a.Description,
                    a.DeviceId,
                    a.DeviceName,
                    a.TagId,
                    a.TagName,
                    a.IsAcknowledged,
                    a.StationId
                })
                .ToListAsync(cancellationToken);

            var items = raw.Select(a =>
            {
                var type = string.IsNullOrWhiteSpace(a.Type) ? "ERROR" : a.Type.Trim();
                return new ActiveAlarmRowDto
                {
                    Id = a.Id,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Type = type,
                    Title = BuildEventTitle(a.Description, a.DeviceName, a.TagName, type),
                    Description = a.Description,
                    DeviceId = a.DeviceId,
                    DeviceName = a.DeviceName,
                    TagId = a.TagId,
                    TagName = a.TagName,
                    IsAcknowledged = a.IsAcknowledged,
                    StationId = a.StationId ?? stationId
                };
            }).ToList();

            return Result<PaginationResult<ActiveAlarmRowDto>>.Success(
                PaginationResult<ActiveAlarmRowDto>.Create(items, total, query));
        });

    public Task<Result<byte[]>> ExportReportTableExcelAsync(
        long stationId,
        StationReportTableQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            query.PageNumber = 1;
            query.PageSize = MaxExportRows;
            var tableResult = await GetReportTableAsync(stationId, query, cancellationToken);
            if (tableResult.IsFailure)
                return Result<byte[]>.Failure(tableResult.ErrorCode, tableResult.Errors);

            var table = tableResult.Value!;
            var headers = new List<string> { "Thời gian" };
            headers.AddRange(table.Columns.Select(c => c.Header));

            var flat = table.Items.Select(item =>
            {
                var cells = new string[headers.Count];
                cells[0] = item.Time.ToOffset(TimeSpan.FromHours(7))
                    .ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var col = table.Columns[i];
                    cells[i + 1] = item.Values.TryGetValue(col.Key, out var v) && v is not null
                        ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? ""
                        : "";
                }
                return new ExportCellRow { Cells = cells, Headers = headers };
            }).ToList();

            var columns = headers
                .Select((h, i) => new ExcelColumn<ExportCellRow>(h, row => row.Cells.Length > i ? row.Cells[i] : ""))
                .ToList();

            var bytes = await importExport.ExportExcelAsync(flat, columns, "BaoCao", cancellationToken);
            await AuditExportAsync("StationReport", stationId.ToString(CultureInfo.InvariantCulture), bytes.Length, cancellationToken);
            return Result<byte[]>.Success(bytes);
        });

    public Task<Result<byte[]>> ExportEventHistoryExcelAsync(
        long stationId,
        StationEventHistoryQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            query.PageNumber = 1;
            query.PageSize = MaxExportRows;
            var result = await GetEventHistoryAsync(stationId, query, cancellationToken);
            if (result.IsFailure)
                return Result<byte[]>.Failure(result.ErrorCode, result.Errors);

            var rows = result.Value!.Items.Select(r => new
            {
                ThoiGian = r.StartTime.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                Loai = r.Type,
                TieuDe = r.Title,
                MoTa = r.Description,
                ThietBi = r.DeviceName,
                Tag = r.TagName,
                KetThuc = r.EndTime?.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
            });

            var bytes = await importExport.ExportExcelAsync(rows, "SuKien", cancellationToken);
            await AuditExportAsync("EventHistory", stationId.ToString(CultureInfo.InvariantCulture), bytes.Length, cancellationToken);
            return Result<byte[]>.Success(bytes);
        });

    public Task<Result<byte[]>> ExportActiveAlarmsExcelAsync(
        long stationId,
        ActiveAlarmQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            query.PageNumber = 1;
            query.PageSize = MaxExportRows;
            var result = await GetActiveAlarmsAsync(stationId, query, cancellationToken);
            if (result.IsFailure)
                return Result<byte[]>.Failure(result.ErrorCode, result.Errors);

            var rows = result.Value!.Items.Select(r => new
            {
                ThoiGian = r.StartTime.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                Loai = r.Type,
                TieuDe = r.Title,
                MoTa = r.Description,
                ThietBi = r.DeviceName,
                Tag = r.TagName,
                DaXacNhan = r.IsAcknowledged ? "Có" : "Không"
            });

            var bytes = await importExport.ExportExcelAsync(rows, "LoiTonTai", cancellationToken);
            await AuditExportAsync("ActiveAlarms", stationId.ToString(CultureInfo.InvariantCulture), bytes.Length, cancellationToken);
            return Result<byte[]>.Success(bytes);
        });

    private sealed class ExportCellRow
    {
        public required string[] Cells { get; init; }
        public required IReadOnlyList<string> Headers { get; init; }
    }

    private Task AuditExportAsync(string entityType, string entityId, int sizeBytes, CancellationToken cancellationToken) =>
        systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.Export,
            EventType = AuditEventType.DataExport,
            Status = AuditStatus.Success,
            Module = "Export",
            EntityType = entityType,
            EntityId = entityId,
            Description = $"Exported {entityType} to Excel.",
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username,
            AdditionalData = new Dictionary<string, object?>
            {
                ["format"] = "xlsx",
                ["sizeBytes"] = sizeBytes,
                ["maxRows"] = MaxExportRows
            }
        }, cancellationToken);

    public Task<Result<StationElectricalDto>> GetElectricalAsync(
        long stationId,
        StationElectricalQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
        var station = await db.Stations.AsNoTracking()
            .Where(s => s.Id == stationId)
            .Select(s => new StationElectricalStationDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (station is null)
            return Result<StationElectricalDto>.Failure(ScadaErrorCodes.StationNotFound, ScadaApiMessages.StationNotFoundFor(stationId));

        var devicesQuery = db.Devices.AsNoTracking()
            .Where(d => d.Plc.StationId == stationId);

        if (query.DeviceId is { } deviceId)
            devicesQuery = devicesQuery.Where(d => d.Id == deviceId);

        if (!string.IsNullOrWhiteSpace(query.DeviceType))
            devicesQuery = devicesQuery.Where(d => d.DeviceType == query.DeviceType);

        var devices = await devicesQuery
            .OrderBy(d => d.Code)
            .Select(d => new DeviceElectricalEquipmentDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = string.IsNullOrWhiteSpace(d.DisplayName) ? d.Name : d.DisplayName
            })
            .ToListAsync(cancellationToken);

        if (query.DeviceId is not null && devices.Count == 0)
        {
            return Result<StationElectricalDto>.Failure(
                "Device.NotFound",
                $"Device '{query.DeviceId}' was not found in station '{stationId}'.");
        }

        var deviceIds = devices.Select(d => d.Id).ToList();
        var tags = deviceIds.Count == 0
            ? []
            : await db.Tags.AsNoTracking()
                .Where(t => deviceIds.Contains(t.DeviceId))
                .Select(t => new { t.Id, t.DeviceId, t.Code, t.TagName, t.Unit })
                .ToListAsync(cancellationToken);

        var tagIds = tags.Select(t => t.Id).ToList();
        var latestByTagId = await LoadLatestHistoryAsync(tagIds, cancellationToken);

        var tagsByDevice = tags.GroupBy(t => t.DeviceId).ToDictionary(g => g.Key, g => g.ToList());

        var items = devices.Select(equipment =>
        {
            tagsByDevice.TryGetValue(equipment.Id, out var deviceTags);
            deviceTags ??= [];

            var parameters = ElectricalParameterCatalog.Definitions.Select(def =>
            {
                var matched = deviceTags.FirstOrDefault(t =>
                    ElectricalParameterCatalog.Matches(def, t.Code) ||
                    ElectricalParameterCatalog.Matches(def, t.TagName));

                double? value = null;
                DateTimeOffset? timestamp = null;
                if (matched is not null && latestByTagId.TryGetValue(matched.Id, out var latest))
                {
                    value = latest.Value;
                    timestamp = latest.Time;
                }

                return new ElectricalParameterDto
                {
                    Key = def.Key,
                    Label = def.Label,
                    TagId = matched?.Id,
                    Code = matched?.Code,
                    Value = value,
                    Unit = matched?.Unit ?? def.DefaultUnit,
                    Timestamp = timestamp
                };
            }).ToList();

            return new DeviceElectricalDto
            {
                Equipment = equipment,
                Parameters = parameters
            };
        }).ToList();

        return Result<StationElectricalDto>.Success(new StationElectricalDto
        {
            Station = station,
            Items = items
        });
        });

    public Task<Result<StationSchematicDto>> GetSchematicAsync(
        long stationId,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
        var station = await db.Stations.AsNoTracking()
            .Where(s => s.Id == stationId)
            .Select(s => new StationElectricalStationDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (station is null)
            return Result<StationSchematicDto>.Failure(ScadaErrorCodes.StationNotFound, ScadaApiMessages.StationNotFoundFor(stationId));

            var devices = await db.Devices.AsNoTracking()
            .Where(d => d.Plc.StationId == stationId && d.DeviceType == "Pump")
            .OrderBy(d => d.Code)
            .Select(d => new
            {
                d.Id,
                d.Code,
                Name = string.IsNullOrWhiteSpace(d.DisplayName) ? d.Name : d.DisplayName,
                d.Description
            })
            .ToListAsync(cancellationToken);

        // Fallback: if no Pump typed devices, take all devices of the station.
        if (devices.Count == 0)
        {
            devices = await db.Devices.AsNoTracking()
                .Where(d => d.Plc.StationId == stationId)
                .OrderBy(d => d.Code)
                .Select(d => new
                {
                    d.Id,
                    d.Code,
                    Name = string.IsNullOrWhiteSpace(d.DisplayName) ? d.Name : d.DisplayName,
                    d.Description
                })
                .ToListAsync(cancellationToken);
        }

        // Schematic UI shows main pumps 1–10 only (exclude priming Pump11/12).
        devices = devices
            .Where(d =>
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    d.Code ?? string.Empty, @"^Pump(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return m.Success && int.TryParse(m.Groups[1].Value, out var n) && n is >= 1 and <= 10;
            })
            .ToList();

        var allStationDevices = await db.Devices.AsNoTracking()
            .Where(d => d.Plc.StationId == stationId)
            .Select(d => new { d.Id, d.Code, d.DeviceType })
            .ToListAsync(cancellationToken);

        static int? MeterIndex(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                code, @"Met+er(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > 0 ? n : null;
        }

        var metersByIndex = allStationDevices
            .Select(d => (d.Id, Index: MeterIndex(d.Code)))
            .Where(x => x.Index is > 0)
            .GroupBy(x => x.Index!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        var meterDeviceIds = allStationDevices
            .Where(d => d.Code.Contains("Meter", StringComparison.OrdinalIgnoreCase)
                        || d.Code.Contains("Metter", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(d.DeviceType, "PowerMeter", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.Id)
            .ToList();

        var relatedIds = devices.Select(d => d.Id).Concat(meterDeviceIds).Distinct().ToList();
        var tags = relatedIds.Count == 0
            ? []
            : await db.Tags.AsNoTracking()
                .Where(t => relatedIds.Contains(t.DeviceId))
                .Select(t => new { t.Id, t.DeviceId, t.Code, t.TagName, t.Unit })
                .ToListAsync(cancellationToken);

        var tagIds = tags.Select(t => t.Id).ToList();
        var latestByTagId = await LoadLatestHistoryAsync(tagIds, cancellationToken);
        var tagsByDevice = tags.GroupBy(t => t.DeviceId).ToDictionary(g => g.Key, g => g.ToList());

        var pumps = new List<SchematicPumpDto>();
        var index = 0;
        foreach (var device in devices)
        {
            index++;
            tagsByDevice.TryGetValue(device.Id, out var deviceTags);
            deviceTags ??= [];

            var branchId = TryExtractBranchNumber(device.Name, device.Code) ?? index;
            var meterIds = metersByIndex.TryGetValue(branchId, out var mids) ? mids : [];
            var meterTags = meterIds
                .SelectMany(id => tagsByDevice.TryGetValue(id, out var list) ? list : [])
                .ToList();
            var prefix = $"Meter{branchId}_";
            var prefixed = tags.Where(t =>
                t.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || t.TagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            var measureTags = deviceTags.Concat(meterTags).Concat(prefixed)
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .ToList();

            double? Measure(string key)
            {
                var def = SchematicParameterCatalog.MeasureDefinitions.First(d => d.Key == key);
                var matched = measureTags
                    .Where(t =>
                        SchematicParameterCatalog.Matches(def, t.Code) ||
                        SchematicParameterCatalog.Matches(def, t.TagName))
                    .Select(t => latestByTagId.TryGetValue(t.Id, out var latest) ? (double?)latest.Value : null)
                    .FirstOrDefault(v => v is not null);
                return matched;
            }

            double? MeasureElec(string key)
            {
                var def = ElectricalParameterCatalog.Definitions.First(d => d.Key == key);
                return measureTags
                    .Where(t =>
                        ElectricalParameterCatalog.Matches(def, t.Code) ||
                        ElectricalParameterCatalog.Matches(def, t.TagName))
                    .Select(t => latestByTagId.TryGetValue(t.Id, out var latest) ? (double?)latest.Value : null)
                    .FirstOrDefault(v => v is not null);
            }

            (double? value, string? code) StatusRaw(string key)
            {
                var def = SchematicParameterCatalog.StatusDefinitions.First(d => d.Key == key);
                var matched = deviceTags.FirstOrDefault(t =>
                    SchematicParameterCatalog.Matches(def, t.Code) ||
                    SchematicParameterCatalog.Matches(def, t.TagName));
                if (matched is null) return (null, null);
                latestByTagId.TryGetValue(matched.Id, out var latest);
                return (latest.Value, matched.TagName);
            }

            var rated = Measure("ratedPowerKw") ?? 160;
            var (motorVal, _) = StatusRaw("motorStatus");
            var (kdmVal, kdmRaw) = StatusRaw("kdmStatus");
            var (lockVal, lockRaw) = StatusRaw("lockStatus");
            var (faultVal, _) = StatusRaw("faultStatus");
            var (stopVal, _) = StatusRaw("stopStatus");
            var (maintVal, _) = StatusRaw("maintenanceStatus");

            var motorStatus = ScadaStatusMapper.ResolveFromFeedback(
                AsBool(faultVal),
                AsBool(maintVal),
                AsBool(motorVal),
                AsBool(stopVal));
            if (motorStatus == ScadaStatusCodes.Unknown)
                motorStatus = SchematicParameterCatalog.MapMotorStatus(motorVal);

            var kdmStatus = SchematicParameterCatalog.ResolveKdmStatus(
                motorStatus,
                SchematicParameterCatalog.MapKdmStatus(kdmVal ?? motorVal, kdmRaw));
            var lockStatus = SchematicParameterCatalog.MapLockStatus(
                lockVal ?? (motorStatus == ScadaStatusCodes.Running ? 1 : 0),
                lockRaw);

            var currentA = Measure("currentA") ?? MeasureElec("currentA");
            var runtimeRaw = Measure("runtimeH");
            // Time_Run_M is minutes; Total_Time_Run_H is hours.
            var runtimeH = runtimeRaw;
            if (runtimeH is > 24 * 30)
                runtimeH = runtimeH / 60.0; // likely minutes mis-tagged

            pumps.Add(new SchematicPumpDto
            {
                Id = branchId,
                DeviceId = device.Id,
                Code = device.Code,
                Label = $"Bơm {branchId}",
                PowerKw = rated,
                MccbCode = $"MCCB{branchId}",
                MotorStatus = motorStatus,
                KdmStatus = kdmStatus,
                LockStatus = lockStatus,
                I1 = Measure("i1") ?? (currentA is null ? null : Math.Round(currentA.Value * 0.95, 2)),
                I2 = Measure("i2") ?? (currentA is null ? null : Math.Round(currentA.Value * 1.02, 2)),
                I3 = Measure("i3") ?? (currentA is null ? null : Math.Round(currentA.Value * 0.98, 2)),
                V1 = Measure("v1") ?? MeasureElec("voltageRs"),
                V2 = Measure("v2") ?? MeasureElec("voltageSt"),
                V3 = Measure("v3") ?? MeasureElec("voltageTr"),
                CurrentA = currentA,
                RuntimeH = runtimeH is null ? null : Math.Round(runtimeH.Value, 1)
            });
        }

        // Keep stable schematic order: higher branch id first (FE mock: 10 → 1).
        pumps = pumps.OrderByDescending(p => p.Id).ToList();

        return Result<StationSchematicDto>.Success(new StationSchematicDto
        {
            Station = station,
            Layout = new SchematicLayoutDto(),
            Pumps = pumps
        });
        });

    public Task<Result<StationDeviceCardsDto>> GetDeviceCardsAsync(
        long stationId,
        StationDeviceCardsQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
        // 1) scada.station by id
        var station = await db.Stations.AsNoTracking()
            .Where(s => s.Id == stationId)
            .Select(s => new StationDetailDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Description = s.Description,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (station is null)
            return Result<StationDeviceCardsDto>.Failure(ScadaErrorCodes.StationNotFound, ScadaApiMessages.StationNotFoundFor(stationId));

        // 2) scada.device JOIN scada.plc ON device.plc_id = plc.id WHERE plc.station_id = @stationId
        var devicesQuery = db.Devices.AsNoTracking()
            .Where(d => d.Plc.StationId == stationId);

        if (query.DeviceId is { } deviceId)
            devicesQuery = devicesQuery.Where(d => d.Id == deviceId);
        if (!string.IsNullOrWhiteSpace(query.DeviceType))
            devicesQuery = devicesQuery.Where(d => d.DeviceType == query.DeviceType);
        if (query.IsEnable is { } isEnable)
            devicesQuery = devicesQuery.Where(d => d.IsActive == isEnable);

        // Materialize device + plc (không đọc ip_address kiểu inet qua EF string)
        var deviceRows = await devicesQuery
            .OrderBy(d => d.Code)
            .Select(d => new
            {
                Device = new DeviceDto
                {
                    Id = d.Id,
                    PlcId = d.PlcId,
                    PlcCode = d.Plc.Code,
                    Code = d.Code,
                    Name = d.Name,
                    DisplayName = d.DisplayName,
                    DeviceType = d.DeviceType,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                },
                Plc = new PlcDto
                {
                    Id = d.Plc.Id,
                    StationId = d.Plc.StationId,
                    StationCode = d.Plc.Station.Code,
                    Code = d.Plc.Code,
                    Name = d.Plc.Name,
                    PlcType = d.Plc.PlcType,
                    IpAddress = d.Plc.IpAddress,
                    Rack = d.Plc.Rack,
                    Slot = d.Plc.Slot,
                    Port = d.Plc.Port,
                    PollingInterval = d.Plc.PollingInterval,
                    ReconnectInterval = d.Plc.ReconnectInterval,
                    Timeout = d.Plc.Timeout,
                    MaxConnection = d.Plc.MaxConnection,
                    IsEnable = d.Plc.IsEnable,
                    Description = d.Plc.Description,
                    CreatedAt = d.Plc.CreatedAt,
                    UpdatedAt = d.Plc.UpdatedAt
                }
            })
            .ToListAsync(cancellationToken);

        var devices = deviceRows;

        var deviceIds = devices.Select(x => x.Device.Id).ToList();

        // 3) scada.tag WHERE device_id IN (...)
        var tags = deviceIds.Count == 0
            ? []
            : await db.Tags.AsNoTracking()
                .Where(t => deviceIds.Contains(t.DeviceId))
                .OrderBy(t => t.Code)
                .Select(t => new DeviceCardTagReadingDto
                {
                    Id = t.Id,
                    PlcId = t.PlcId,
                    DeviceId = t.DeviceId,
                    Code = t.Code,
                    TagName = t.TagName,
                    DisplayName = t.DisplayName,
                    Address = t.Address,
                    DataType = t.DataType,
                    Unit = t.Unit,
                    Scale = t.Scale,
                    OffsetValue = t.OffsetValue,
                    ReadOnly = t.ReadOnly,
                    WriteEnable = t.WriteEnable,
                    EnableRealtime = t.EnableRealtime,
                    EnableAlarm = t.EnableAlarm,
                    Description = t.Description
                })
                .ToListAsync(cancellationToken);

        // 4) history.history_1s JOIN by tag_id (latest value)
        var latestByTagId = await LoadLatestHistoryAsync(tags.Select(t => t.Id).ToList(), cancellationToken);
        foreach (var tag in tags)
        {
            if (!latestByTagId.TryGetValue(tag.Id, out var latest)) continue;
            tag.Value = latest.Value;
            tag.Timestamp = latest.Time;
        }

        var tagsByDevice = tags.GroupBy(t => t.DeviceId).ToDictionary(g => g.Key, g => (IReadOnlyList<DeviceCardTagReadingDto>)g.ToList());

        var items = devices.Select(row => new DeviceCardItemDto
        {
            Plc = row.Plc,
            Device = row.Device,
            Tags = tagsByDevice.TryGetValue(row.Device.Id, out var deviceTags) ? deviceTags : []
        }).ToList();

        return Result<StationDeviceCardsDto>.Success(new StationDeviceCardsDto
        {
            Station = station,
            Items = items
        });
        });

    public Task<Result<StationDeviceMonitorDto>> GetDeviceMonitorAsync(
        long stationId,
        StationDeviceMonitorQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);

            if (!stationExists)
                return Result<StationDeviceMonitorDto>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            var allDevices = await db.Devices.AsNoTracking()
                .Where(d => d.Plc.StationId == stationId)
                .OrderBy(d => d.Code)
                .Select(d => new { d.Id, d.Code, d.Name, d.DisplayName, d.DeviceType })
                .ToListAsync(cancellationToken);

            static int? PumpIndex(string? code)
            {
                if (string.IsNullOrWhiteSpace(code)) return null;
                var m = System.Text.RegularExpressions.Regex.Match(code, @"^Pump(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : null;
            }

            static int? MeterIndex(string? code)
            {
                if (string.IsNullOrWhiteSpace(code)) return null;
                var m = System.Text.RegularExpressions.Regex.Match(
                    code, @"Met+er(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : null;
            }

            var pumps = allDevices
                .Select(d => (Device: d, Index: PumpIndex(d.Code)))
                // Main station pumps only (Pump1–Pump10). Exclude priming Pump11/12.
                .Where(x => x.Index is >= 1 and <= 10)
                .OrderBy(x => x.Index)
                .ToList();

            if (query.DeviceId is { } filterDeviceId)
                pumps = pumps.Where(p => p.Device.Id == filterDeviceId).ToList();

            var levelDevice = allDevices.FirstOrDefault(d =>
                string.Equals(d.Code, "Level", StringComparison.OrdinalIgnoreCase)
                || string.Equals(d.DeviceType, "Sensor", StringComparison.OrdinalIgnoreCase)
                   && d.Name.Contains("nước", StringComparison.OrdinalIgnoreCase));

            var metersByIndex = allDevices
                .Select(d => (Device: d, Index: MeterIndex(d.Code)))
                .Where(x => x.Index is > 0)
                .GroupBy(x => x.Index!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Device.Id).ToList());

            var meterDeviceIds = allDevices
                .Where(d => d.Code.Contains("Meter", StringComparison.OrdinalIgnoreCase)
                            || d.Code.Contains("Metter", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(d.DeviceType, "PowerMeter", StringComparison.OrdinalIgnoreCase))
                .Select(d => d.Id)
                .ToList();

            var relatedIds = pumps.Select(p => p.Device.Id)
                .Concat(levelDevice is null ? [] : new[] { levelDevice.Id })
                .Concat(metersByIndex.Values.SelectMany(x => x))
                .Concat(meterDeviceIds)
                .Distinct()
                .ToList();

            var tagRows = relatedIds.Count == 0
                ? new List<DeviceMonitorTagRow>()
                : await db.Tags.AsNoTracking()
                    .Where(t => relatedIds.Contains(t.DeviceId))
                    .Select(t => new DeviceMonitorTagRow
                    {
                        Id = t.Id,
                        DeviceId = t.DeviceId,
                        Code = t.Code,
                        TagName = t.TagName
                    })
                    .ToListAsync(cancellationToken);

            var latestByTagId = await LoadLatestHistoryAsync(tagRows.Select(t => t.Id).ToList(), cancellationToken);
            foreach (var tag in tagRows)
            {
                if (!latestByTagId.TryGetValue(tag.Id, out var latest)) continue;
                tag.Value = latest.Value;
                tag.Timestamp = latest.Time;
            }

            var tagsByDevice = tagRows
                .GroupBy(t => t.DeviceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<DeviceMonitorTagRow> TagsFor(long deviceId) =>
                tagsByDevice.TryGetValue(deviceId, out var list) ? list : [];

            double? Pick(IEnumerable<DeviceMonitorTagRow> tags, string key)
            {
                var def = DeviceMonitorParameterCatalog.Definitions.First(d => d.Key == key);
                return tags
                    .Where(t =>
                        DeviceMonitorParameterCatalog.Matches(def, t.Code) ||
                        DeviceMonitorParameterCatalog.Matches(def, t.TagName))
                    .Select(t => t.Value)
                    .FirstOrDefault(v => v is not null);
            }

            double? PickElec(IEnumerable<DeviceMonitorTagRow> tags, string key)
            {
                var def = ElectricalParameterCatalog.Definitions.First(d => d.Key == key);
                return tags
                    .Where(t =>
                        ElectricalParameterCatalog.Matches(def, t.Code) ||
                        ElectricalParameterCatalog.Matches(def, t.TagName))
                    .Select(t => t.Value)
                    .FirstOrDefault(v => v is not null);
            }

            bool? Feedback(IEnumerable<DeviceMonitorTagRow> tags, string key)
            {
                var v = Pick(tags, key);
                return v is null ? null : v != 0;
            }

            var levelTags = levelDevice is null ? [] : TagsFor(levelDevice.Id);
            var river = Pick(levelTags, "levelRiver");
            var basin = Pick(levelTags, "levelBasin");

            var items = pumps.Select(p =>
            {
                var deviceTags = TagsFor(p.Device.Id);
                var meterIds = metersByIndex.TryGetValue(p.Index!.Value, out var ids) ? ids : [];
                var meterTags = meterIds.SelectMany(TagsFor).ToList();
                // Also include tags named Meter{N}_* / Metter{N}_* from any device (seed duplicates).
                var prefix = $"Meter{p.Index}_";
                var prefix2 = $"Metter{p.Index}_";
                var prefixed = tagRows.Where(t =>
                    t.Code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    || t.Code.StartsWith(prefix2, StringComparison.OrdinalIgnoreCase)
                    || t.TagName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    || t.TagName.StartsWith(prefix2, StringComparison.OrdinalIgnoreCase)).ToList();
                meterTags = meterTags.Concat(prefixed).GroupBy(t => t.Id).Select(g => g.First()).ToList();
                var combined = deviceTags.Concat(meterTags).Concat(levelTags).ToList();

                var status = ScadaStatusMapper.ResolveFromFeedback(
                    Feedback(deviceTags, "faultStatus"),
                    Feedback(deviceTags, "maintenanceStatus"),
                    Feedback(deviceTags, "motorStatus"),
                    Feedback(deviceTags, "stopStatus"));

                // Time_* tags in Excel are minutes; Total_Time_Run_H is hours.
                var instantMin = Pick(deviceTags, "runtimeInstant");
                var totalMin = deviceTags.FirstOrDefault(t =>
                    DeviceMonitorParameterCatalog.Matches(
                        DeviceMonitorParameterCatalog.Definitions.First(d => d.Key == "runtimeTotal"),
                        t.Code) &&
                    t.Code.Contains("Time_Run_M", StringComparison.OrdinalIgnoreCase))?.Value
                    ?? Pick(deviceTags, "runtimeTotal");
                var totalH = deviceTags.FirstOrDefault(t =>
                    string.Equals(t.Code, "Total_Time_Run_H", StringComparison.OrdinalIgnoreCase)
                    || t.TagName.Contains("Total_Time_Run_H", StringComparison.OrdinalIgnoreCase))?.Value;

                var displayName = string.IsNullOrWhiteSpace(p.Device.DisplayName) ? p.Device.Name : p.Device.DisplayName;
                if (!displayName.Contains("Bơm", StringComparison.OrdinalIgnoreCase)
                    && !displayName.Contains("Bom", StringComparison.OrdinalIgnoreCase))
                    displayName = $"Bơm {p.Index}";

                return new DeviceMonitorItemDto
                {
                    DeviceId = p.Device.Id,
                    Name = displayName,
                    RatedPowerKw = Pick(deviceTags, "ratedPowerKw") ?? 160,
                    Status = status,
                    WindingTempActual = new DeviceMonitorAbcValueDto
                    {
                        A = Pick(deviceTags, "tempCoilA"),
                        B = Pick(deviceTags, "tempCoilB"),
                        C = Pick(deviceTags, "tempCoilC")
                    },
                    WindingTempAllowed = new DeviceMonitorAbcValueDto
                    {
                        A = Pick(deviceTags, "tempCoilAMax"),
                        B = Pick(deviceTags, "tempCoilBMax"),
                        C = Pick(deviceTags, "tempCoilCMax")
                    },
                    BearingTop = new DeviceMonitorActualAllowedDto
                    {
                        Actual = Pick(deviceTags, "bearingTop"),
                        Allowed = Pick(deviceTags, "bearingTopMax")
                    },
                    BearingBottom = new DeviceMonitorActualAllowedDto
                    {
                        Actual = Pick(deviceTags, "bearingBottom"),
                        Allowed = Pick(deviceTags, "bearingBottomMax")
                    },
                    WaterLevel = new DeviceMonitorWaterLevelDto
                    {
                        River = river ?? Pick(combined, "levelRiver"),
                        Basin = basin ?? Pick(combined, "levelBasin")
                    },
                    Runtime = new DeviceMonitorRuntimeDto
                    {
                        InstantH = instantMin is null ? null : instantMin / 60.0,
                        TotalH = totalH ?? (totalMin is null ? null : totalMin / 60.0)
                    },
                    Electrical = new DeviceMonitorElectricalDto
                    {
                        VoltageRs = PickElec(meterTags, "voltageRs") ?? PickElec(deviceTags, "voltageRs"),
                        VoltageSt = PickElec(meterTags, "voltageSt") ?? PickElec(deviceTags, "voltageSt"),
                        VoltageTr = PickElec(meterTags, "voltageTr") ?? PickElec(deviceTags, "voltageTr"),
                        CurrentA = PickElec(meterTags, "currentA") ?? PickElec(deviceTags, "currentA"),
                        PowerFactor = PickElec(meterTags, "powerFactor") ?? PickElec(deviceTags, "powerFactor"),
                        FrequencyHz = PickElec(meterTags, "frequencyHz") ?? PickElec(deviceTags, "frequencyHz"),
                        PowerKw = PickElec(meterTags, "powerKw") ?? PickElec(deviceTags, "powerKw"),
                        EnergyKwh = PickElec(meterTags, "energyKwh") ?? PickElec(deviceTags, "energyKwh")
                    }
                };
            }).ToList();

            return Result<StationDeviceMonitorDto>.Success(new StationDeviceMonitorDto
            {
                Items = items
            });
        });

    private sealed class DeviceMonitorTagRow
    {
        public long Id { get; set; }
        public long DeviceId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string TagName { get; set; } = string.Empty;
        public double? Value { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    private static bool? AsBool(double? value) => value is null ? null : value != 0;

    public Task<Result<PaginationResult<WaterLevelReportRowDto>>> GetWaterLevelReportAsync(
        long stationId,
        StationWaterLevelReportQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<PaginationResult<WaterLevelReportRowDto>>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            if (!TryResolveWaterLevelRange(query, out var from, out var to, out var rangeError))
                return Result<PaginationResult<WaterLevelReportRowDto>>.Failure(
                    rangeError!.Value.Code,
                    rangeError.Value.Message);

            var tagsQuery = db.Tags.AsNoTracking()
                .Where(t => t.Device.Plc.StationId == stationId);

            if (query.DeviceId is { } deviceId)
                tagsQuery = tagsQuery.Where(t => t.DeviceId == deviceId);
            if (!string.IsNullOrWhiteSpace(query.DeviceType))
                tagsQuery = tagsQuery.Where(t => t.Device.DeviceType == query.DeviceType);

            var tagRows = await tagsQuery
                .Select(t => new { t.Id, t.Code, t.TagName, t.DeviceId })
                .ToListAsync(cancellationToken);

            // 1 tag / key — ưu tiên device_id nhỏ nhất
            var tagIdByKey = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var def in WaterLevelReportCatalog.Definitions)
            {
                var matched = tagRows
                    .Where(t =>
                        WaterLevelReportCatalog.Matches(def, t.Code) ||
                        WaterLevelReportCatalog.Matches(def, t.TagName))
                    .OrderBy(t => t.DeviceId)
                    .ThenBy(t => t.Id)
                    .FirstOrDefault();
                if (matched is not null)
                    tagIdByKey[def.Key] = matched.Id;
            }

            if (tagIdByKey.Count == 0)
                return Result<PaginationResult<WaterLevelReportRowDto>>.Success(
                    PaginationResult<WaterLevelReportRowDto>.Empty(query));

            var keyByTagId = tagIdByKey.ToDictionary(kv => kv.Value, kv => kv.Key);
            var tagIds = tagIdByKey.Values.ToList();

            // Nguồn: history.history_30s — sắp xếp theo thời gian giảm dần
            var raw = await db.History30s.AsNoTracking()
                .Where(h => tagIds.Contains(h.TagId) && h.Time >= from && h.Time <= to)
                .Select(h => new { h.Time, h.TagId, h.Value })
                .ToListAsync(cancellationToken);

            var groups = raw
                .GroupBy(s => s.Time)
                .OrderByDescending(g => g.Key)
                .ToList();

            var total = groups.Count;
            var pageGroups = groups
                .Skip(query.Skip)
                .Take(query.PageSize)
                .ToList();

            var items = pageGroups.Select(g =>
            {
                var row = new WaterLevelReportRowDto { Time = g.Key };
                foreach (var sample in g)
                {
                    if (!keyByTagId.TryGetValue(sample.TagId, out var key)) continue;
                    switch (key)
                    {
                        case "riverLevel": row.RiverLevel = sample.Value; break;
                        case "discharge1": row.Discharge1 = sample.Value; break;
                        case "discharge2": row.Discharge2 = sample.Value; break;
                        case "discharge3": row.Discharge3 = sample.Value; break;
                        case "discharge4": row.Discharge4 = sample.Value; break;
                        case "discharge5": row.Discharge5 = sample.Value; break;
                        case "discharge6": row.Discharge6 = sample.Value; break;
                        case "discharge7": row.Discharge7 = sample.Value; break;
                        case "discharge8": row.Discharge8 = sample.Value; break;
                        case "discharge9": row.Discharge9 = sample.Value; break;
                        case "discharge10": row.Discharge10 = sample.Value; break;
                    }
                }
                return row;
            }).ToList();

            return Result<PaginationResult<WaterLevelReportRowDto>>.Success(
                PaginationResult<WaterLevelReportRowDto>.Create(items, total, query));
        });

    public Task<Result<IReadOnlyList<StationReportDeviceOptionDto>>> GetReportDeviceOptionsAsync(
        long stationId,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<IReadOnlyList<StationReportDeviceOptionDto>>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            var devices = await db.Devices.AsNoTracking()
                .Where(d => d.Plc.StationId == stationId && d.IsActive)
                .Select(d => new { d.Id, d.Code, d.Name, d.DeviceType })
                .ToListAsync(cancellationToken);

            static int? PumpIndex(string? code)
            {
                if (string.IsNullOrWhiteSpace(code)) return null;
                var m = System.Text.RegularExpressions.Regex.Match(
                    code, @"^Pump(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : null;
            }

            static string DisplayName(string? name, string? code) =>
                string.IsNullOrWhiteSpace(name) ? (code ?? string.Empty) : name.Trim();

            var options = new List<StationReportDeviceOptionDto>();

            // Level / Sensor trước (UI: Mức nước).
            foreach (var d in devices
                         .Where(x =>
                             string.Equals(x.DeviceType, "Sensor", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(x.Code, "Level", StringComparison.OrdinalIgnoreCase)
                             || (x.Code ?? string.Empty).Contains("Level", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
            {
                options.Add(new StationReportDeviceOptionDto
                {
                    Id = d.Id,
                    Name = DisplayName(d.Name, d.Code)
                });
            }

            // Pump1–10 giống dropdown đồ thị.
            foreach (var x in devices
                         .Select(d => (Device: d, Index: PumpIndex(d.Code)))
                         .Where(t => t.Index is >= 1 and <= 10)
                         .OrderBy(t => t.Index))
            {
                options.Add(new StationReportDeviceOptionDto
                {
                    Id = x.Device.Id,
                    Name = DisplayName(x.Device.Name, x.Device.Code)
                });
            }

            // Đồng hồ điện (PowerMeter / Meter*).
            foreach (var d in devices
                         .Where(x =>
                             string.Equals(x.DeviceType, "PowerMeter", StringComparison.OrdinalIgnoreCase)
                             || (x.Code ?? string.Empty).Contains("Meter", StringComparison.OrdinalIgnoreCase)
                             || (x.Code ?? string.Empty).Contains("Metter", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
            {
                if (options.Any(o => o.Id == d.Id)) continue;
                options.Add(new StationReportDeviceOptionDto
                {
                    Id = d.Id,
                    Name = DisplayName(d.Name, d.Code)
                });
            }

            return Result<IReadOnlyList<StationReportDeviceOptionDto>>.Success(options);
        });

    public Task<Result<StationReportTableDto>> GetReportTableAsync(
        long stationId,
        StationReportTableQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<StationReportTableDto>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            if (query.DeviceId <= 0)
                return Result<StationReportTableDto>.Failure(
                    ScadaErrorCodes.DeviceNotFound,
                    "deviceId is required.");

            if (!TryResolveReportDayRange(
                    query.ReportDate, query.StartTime, query.EndTime,
                    out var from, out var to, out var rangeError))
                return Result<StationReportTableDto>.Failure(
                    rangeError!.Value.Code,
                    rangeError.Value.Message);

            var device = await db.Devices.AsNoTracking()
                .Where(d => d.Id == query.DeviceId && d.Plc.StationId == stationId)
                .Select(d => new { d.Id, d.Code, d.Name, d.DeviceType })
                .FirstOrDefaultAsync(cancellationToken);

            if (device is null)
                return Result<StationReportTableDto>.Failure(
                    ScadaErrorCodes.DeviceNotFound,
                    ScadaApiMessages.DeviceNotFound);

            var columnDefs = StationReportTagCatalog.ResolveColumns(device.DeviceType, device.Code);

            var tags = await db.Tags.AsNoTracking()
                .Where(t => t.DeviceId == device.Id)
                .Select(t => new { t.Id, t.Code, t.TagName })
                .ToListAsync(cancellationToken);

            var columns = new List<StationReportColumnDto>(columnDefs.Count);
            var tagIdByKey = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var def in columnDefs)
            {
                var matched = tags.FirstOrDefault(t =>
                    StationReportTagCatalog.Matches(def, t.Code) ||
                    StationReportTagCatalog.Matches(def, t.TagName));
                columns.Add(new StationReportColumnDto
                {
                    Key = def.Key,
                    Header = def.Header,
                    TagId = matched?.Id,
                    TagCode = matched?.Code
                });
                if (matched is not null)
                    tagIdByKey[def.Key] = matched.Id;
            }

            var empty = new StationReportTableDto
            {
                StationId = stationId,
                DeviceId = device.Id,
                DeviceName = string.IsNullOrWhiteSpace(device.Name) ? device.Code : device.Name,
                DeviceCode = device.Code ?? string.Empty,
                Interval = "30m",
                From = from,
                To = to,
                Columns = columns,
                Items = [],
                TotalCount = 0,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            if (tagIdByKey.Count == 0)
                return Result<StationReportTableDto>.Success(empty);

            var allTagIds = tagIdByKey.Values.Distinct().ToList();
            var keyByTagId = tagIdByKey.ToDictionary(kv => kv.Value, kv => kv.Key);

            var raw = await db.History30m.AsNoTracking()
                .Where(h => allTagIds.Contains(h.TagId) && h.Time >= from && h.Time <= to)
                .Select(h => new { h.Time, h.TagId, h.Value })
                .ToListAsync(cancellationToken);

            var rowMap = new Dictionary<DateTimeOffset, StationReportRowDto>();
            foreach (var sample in raw)
            {
                if (!keyByTagId.TryGetValue(sample.TagId, out var key)) continue;
                if (!rowMap.TryGetValue(sample.Time, out var row))
                {
                    row = new StationReportRowDto { Time = sample.Time };
                    rowMap[sample.Time] = row;
                }
                row.Values[key] = sample.Value;
            }

            var ordered = rowMap.Values
                .OrderByDescending(r => r.Time)
                .ToList();

            var total = ordered.Count;
            var items = ordered
                .Skip(query.Skip)
                .Take(query.PageSize)
                .ToList();

            empty.Items = items;
            empty.TotalCount = total;
            return Result<StationReportTableDto>.Success(empty);
        });

    public Task<Result<IReadOnlyList<StationReportDeviceOptionDto>>> GetEventDeviceOptionsAsync(
        long stationId,
        CancellationToken cancellationToken = default) =>
        GetReportDeviceOptionsAsync(stationId, cancellationToken);

    public Task<Result<PaginationResult<StationEventHistoryRowDto>>> GetEventHistoryAsync(
        long stationId,
        StationEventHistoryQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<PaginationResult<StationEventHistoryRowDto>>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            if (!TryResolveEventDateRange(
                    query.FromDate, query.ToDate,
                    out var from, out var to, out var rangeError))
                return Result<PaginationResult<StationEventHistoryRowDto>>.Failure(
                    rangeError!.Value.Code,
                    rangeError.Value.Message);

            var stationDeviceIds = await db.Devices.AsNoTracking()
                .Where(d => d.Plc.StationId == stationId)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            if (query.DeviceId is { } deviceId && deviceId > 0
                && !stationDeviceIds.Contains(deviceId))
                return Result<PaginationResult<StationEventHistoryRowDto>>.Failure(
                    ScadaErrorCodes.DeviceNotFound,
                    ScadaApiMessages.DeviceNotFound);

            var q = db.AlarmHistories.AsNoTracking()
                .Where(a => a.StartTime >= from && a.StartTime <= to)
                .Where(a =>
                    a.StationId == stationId
                    || (a.DeviceId != null && stationDeviceIds.Contains(a.DeviceId.Value)));

            if (query.DeviceId is { } filterDeviceId && filterDeviceId > 0)
                q = q.Where(a => a.DeviceId == filterDeviceId);

            var category = (query.Category ?? "status").Trim().ToLowerInvariant();
            q = category switch
            {
                "value-change" or "valuechange" or "value_change" => q.Where(a =>
                    (a.EventTypeId != null && (a.EventTypeId == 4 || a.EventTypeId == 5))
                    || (a.Type != null && (
                        a.Type == "START" || a.Type == "STOP" || a.Type == "PUMP"
                        || a.Type == "INFO"))),
                "system" => q.Where(a =>
                    (a.EventTypeId != null && a.EventTypeId == 6)
                    || (a.Type != null && (
                        a.Type == "SYSTEM" || a.Type == "Communication"
                        || a.Type == "EVENT"))),
                // status = Lỗi/Trạng thái (mặc định)
                _ => q.Where(a =>
                    (a.EventTypeId != null && (a.EventTypeId == 1 || a.EventTypeId == 2 || a.EventTypeId == 7))
                    || (a.Type != null && (
                        a.Type == "ERROR" || a.Type == "ALARM" || a.Type == "WARNING"
                        || a.Type == "FAULT")))
            };

            if (query.HasKeyword)
            {
                var keyword = query.Keyword!;
                q = q.Where(a =>
                    (a.DeviceName != null && a.DeviceName.Contains(keyword)) ||
                    (a.TagName != null && a.TagName.Contains(keyword)) ||
                    (a.Description != null && a.Description.Contains(keyword)) ||
                    (a.Type != null && a.Type.Contains(keyword)) ||
                    (a.TroubleshootingGuide != null && a.TroubleshootingGuide.Contains(keyword)));
            }

            var total = await q.CountAsync(cancellationToken);
            var raw = await q
                .OrderByDescending(a => a.StartTime)
                .ThenByDescending(a => a.Id)
                .Skip(query.Skip)
                .Take(query.PageSize)
                .Select(a => new
                {
                    a.Id,
                    a.StartTime,
                    a.EndTime,
                    a.Type,
                    a.Description,
                    a.DeviceId,
                    a.DeviceName,
                    a.TagId,
                    a.TagName,
                    a.EventTypeId,
                    a.IsAcknowledged
                })
                .ToListAsync(cancellationToken);

            var items = raw.Select(a =>
            {
                var type = string.IsNullOrWhiteSpace(a.Type) ? "ERROR" : a.Type.Trim();
                var title = BuildEventTitle(a.Description, a.DeviceName, a.TagName, type);
                return new StationEventHistoryRowDto
                {
                    Id = a.Id,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Type = type,
                    Title = title,
                    Description = a.Description,
                    DeviceId = a.DeviceId,
                    DeviceName = a.DeviceName,
                    TagId = a.TagId,
                    TagName = a.TagName,
                    Username = null,
                    EventTypeId = a.EventTypeId,
                    IsAcknowledged = a.IsAcknowledged
                };
            }).ToList();

            return Result<PaginationResult<StationEventHistoryRowDto>>.Success(
                PaginationResult<StationEventHistoryRowDto>.Create(items, total, query));
        });

    public Task<Result<PaginationResult<PumpTemperatureReportRowDto>>> GetPumpTemperatureReportAsync(
        long stationId,
        StationPumpTemperatureReportQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<PaginationResult<PumpTemperatureReportRowDto>>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            if (!TryResolveReportDayRange(
                    query.ReportDate, query.StartTime, query.EndTime,
                    out var from, out var to, out var rangeError))
                return Result<PaginationResult<PumpTemperatureReportRowDto>>.Failure(
                    rangeError!.Value.Code,
                    rangeError.Value.Message);

            var devicesQuery = db.Devices.AsNoTracking()
                .Where(d => d.Plc.StationId == stationId && d.DeviceType == "Pump");

            if (query.DeviceId is { } deviceId)
                devicesQuery = devicesQuery.Where(d => d.Id == deviceId);

            var devices = await devicesQuery
                .OrderBy(d => d.Code)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync(cancellationToken);

            if (devices.Count == 0)
                return Result<PaginationResult<PumpTemperatureReportRowDto>>.Success(
                    PaginationResult<PumpTemperatureReportRowDto>.Empty(query));

            var deviceIds = devices.Select(d => d.Id).ToList();
            var nameById = devices.ToDictionary(d => d.Id, d => d.Name);

            var tagRows = await db.Tags.AsNoTracking()
                .Where(t => deviceIds.Contains(t.DeviceId))
                .Select(t => new { t.Id, t.DeviceId, t.Code, t.TagName })
                .ToListAsync(cancellationToken);

            // deviceId -> (key -> tagId)
            var tagMap = new Dictionary<long, Dictionary<string, long>>();
            foreach (var pumpDeviceId in deviceIds)
            {
                var map = new Dictionary<string, long>(StringComparer.Ordinal);
                var deviceTags = tagRows.Where(t => t.DeviceId == pumpDeviceId).ToList();
                foreach (var def in PumpTemperatureReportCatalog.Definitions)
                {
                    var matched = deviceTags.FirstOrDefault(t =>
                        PumpTemperatureReportCatalog.Matches(def, t.Code) ||
                        PumpTemperatureReportCatalog.Matches(def, t.TagName));
                    if (matched is not null)
                        map[def.Key] = matched.Id;
                }
                if (map.Count > 0)
                    tagMap[pumpDeviceId] = map;
            }

            var allTagIds = tagMap.Values.SelectMany(m => m.Values).Distinct().ToList();
            if (allTagIds.Count == 0)
                return Result<PaginationResult<PumpTemperatureReportRowDto>>.Success(
                    PaginationResult<PumpTemperatureReportRowDto>.Empty(query));

            var keyByTagId = new Dictionary<long, (long DeviceId, string Key)>();
            foreach (var (devId, map) in tagMap)
            {
                foreach (var (key, tagId) in map)
                    keyByTagId[tagId] = (devId, key);
            }

            var raw = await db.History30s.AsNoTracking()
                .Where(h => allTagIds.Contains(h.TagId) && h.Time >= from && h.Time <= to)
                .Select(h => new { h.Time, h.TagId, h.Value })
                .ToListAsync(cancellationToken);

            // 1 dòng = 1 (time, device)
            var rowMap = new Dictionary<(DateTimeOffset Time, long DeviceId), PumpTemperatureReportRowDto>();
            foreach (var sample in raw)
            {
                if (!keyByTagId.TryGetValue(sample.TagId, out var meta)) continue;
                var rowKey = (sample.Time, meta.DeviceId);
                if (!rowMap.TryGetValue(rowKey, out var row))
                {
                    row = new PumpTemperatureReportRowDto
                    {
                        Time = sample.Time,
                        DeviceId = meta.DeviceId,
                        Pump = nameById.GetValueOrDefault(meta.DeviceId, $"Bơm {meta.DeviceId}")
                    };
                    rowMap[rowKey] = row;
                }

                switch (meta.Key)
                {
                    case "tempA": row.TempA = sample.Value; break;
                    case "tempB": row.TempB = sample.Value; break;
                    case "tempC": row.TempC = sample.Value; break;
                    case "bearingBottom": row.BearingBottom = sample.Value; break;
                    case "bearingTop": row.BearingTop = sample.Value; break;
                }
            }

            var ordered = rowMap.Values
                .OrderByDescending(r => r.Time)
                .ThenBy(r => r.DeviceId)
                .ToList();

            var total = ordered.Count;
            var items = ordered
                .Skip(query.Skip)
                .Take(query.PageSize)
                .ToList();

            return Result<PaginationResult<PumpTemperatureReportRowDto>>.Success(
                PaginationResult<PumpTemperatureReportRowDto>.Create(items, total, query));
        });

    public Task<Result<IReadOnlyList<StationChartDeviceOptionDto>>> GetChartDevicesAsync(
        long stationId,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<IReadOnlyList<StationChartDeviceOptionDto>>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            var devices = await db.Devices.AsNoTracking()
                .Where(d => d.Plc.StationId == stationId && d.IsActive)
                .Where(d => d.DeviceType == "Pump")
                .OrderBy(d => d.Code)
                .Select(d => new { d.Id, d.Code, d.Name })
                .ToListAsync(cancellationToken);

            static int? PumpIndex(string? code)
            {
                if (string.IsNullOrWhiteSpace(code)) return null;
                var m = System.Text.RegularExpressions.Regex.Match(
                    code, @"^Pump(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return m.Success && int.TryParse(m.Groups[1].Value, out var n) ? n : null;
            }

            // Chart nhiệt/dòng: Pump1–10; label = Device.Name, value = Device.Id.
            var options = devices
                .Select(d => (Device: d, Index: PumpIndex(d.Code)))
                .Where(x => x.Index is >= 1 and <= 10)
                .OrderBy(x => x.Index)
                .Select(x => new StationChartDeviceOptionDto
                {
                    Id = x.Device.Id,
                    Name = string.IsNullOrWhiteSpace(x.Device.Name)
                        ? x.Device.Code
                        : x.Device.Name
                })
                .ToList();

            return Result<IReadOnlyList<StationChartDeviceOptionDto>>.Success(options);
        });

    public Task<Result<StationChartHistoryDto>> GetChartHistoryAsync(
        long stationId,
        long deviceId,
        string chart,
        StationChartHistoryQuery query,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            if (!StationChartCatalog.TryGetSeries(chart ?? string.Empty, out var seriesDefs))
                return Result<StationChartHistoryDto>.Failure(
                    ScadaErrorCodes.ChartUnknown,
                    $"Unknown chart '{chart}'. Use temperature or current.");

            var stationExists = await db.Stations.AsNoTracking()
                .AnyAsync(s => s.Id == stationId, cancellationToken);
            if (!stationExists)
                return Result<StationChartHistoryDto>.Failure(
                    ScadaErrorCodes.StationNotFound,
                    ScadaApiMessages.StationNotFoundFor(stationId));

            var deviceOk = await (
                    from d in db.Devices.AsNoTracking()
                    join p in db.Plcs.AsNoTracking() on d.PlcId equals p.Id
                    where d.Id == deviceId && p.StationId == stationId
                    select d.Id)
                .AnyAsync(cancellationToken);
            if (!deviceOk)
                return Result<StationChartHistoryDto>.Failure(
                    ScadaErrorCodes.DeviceNotFound,
                    ScadaApiMessages.DeviceNotFound);

            if (query.From is null || query.To is null)
                return Result<StationChartHistoryDto>.Failure(
                    ScadaErrorCodes.TimeRangeRequired,
                    "Both 'from' and 'to' are required.");

            var from = query.From.Value.ToUniversalTime();
            var to = query.To.Value.ToUniversalTime();
            if (from > to)
                return Result<StationChartHistoryDto>.Failure(
                    ScadaErrorCodes.InvalidTimeRange,
                    "'from' must be less than or equal to 'to'.");

            var span = to - from;
            if (span > TimeSpan.FromHours(24))
                return Result<StationChartHistoryDto>.Failure(
                    ScadaErrorCodes.ChartRangeTooLarge,
                    "Chart time range must not exceed 24 hours.");

            var interval = NormalizeChartInterval(query.Interval);
            // Chart luôn lấy history_1s rồi gộp 1 điểm / 15 phút (tránh dày).
            const int bucketMinutes = 15;

            // Pump tags + related meter tags (I1/I2/I3 often live on MeterN).
            var pumpCode = await db.Devices.AsNoTracking()
                .Where(d => d.Id == deviceId)
                .Select(d => d.Code)
                .FirstOrDefaultAsync(cancellationToken);

            var relatedDeviceIds = new List<long> { deviceId };
            var pumpIndexMatch = System.Text.RegularExpressions.Regex.Match(
                pumpCode ?? string.Empty, @"^Pump(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (pumpIndexMatch.Success && int.TryParse(pumpIndexMatch.Groups[1].Value, out var pumpNo))
            {
                var meterSuffix = pumpNo.ToString();
                // Excel: MeterN; DB seed: MetterN (typo) — nhận cả hai.
                var meterIds = await db.Devices.AsNoTracking()
                    .Where(d => d.Plc.StationId == stationId)
                    .Where(d =>
                        d.Code == "Meter" + meterSuffix ||
                        d.Code == "Metter" + meterSuffix ||
                        d.Code.EndsWith("Meter" + meterSuffix) ||
                        d.Code.EndsWith("Metter" + meterSuffix))
                    .Select(d => d.Id)
                    .ToListAsync(cancellationToken);
                relatedDeviceIds.AddRange(meterIds);
            }

            var tagRows = await db.Tags.AsNoTracking()
                .Where(t => relatedDeviceIds.Contains(t.DeviceId))
                .Select(t => new { t.Id, t.DeviceId, t.Code, t.TagName })
                .ToListAsync(cancellationToken);

            // Prefer tags on the pump device; fall back to meter tags.
            var matched = new Dictionary<string, (long TagId, string Code)>(StringComparer.Ordinal);
            foreach (var def in seriesDefs)
            {
                var onPump = tagRows
                    .Where(t => t.DeviceId == deviceId)
                    .FirstOrDefault(t =>
                        StationChartCatalog.Matches(def, t.Code) ||
                        StationChartCatalog.Matches(def, t.TagName));
                if (onPump is not null)
                {
                    matched[def.Key] = (onPump.Id, onPump.Code);
                    continue;
                }

                var onMeter = tagRows
                    .Where(t => t.DeviceId != deviceId)
                    .FirstOrDefault(t =>
                        StationChartCatalog.Matches(def, t.Code) ||
                        StationChartCatalog.Matches(def, t.TagName));
                if (onMeter is not null)
                    matched[def.Key] = (onMeter.Id, onMeter.Code);
            }

            var measuredDefs = seriesDefs.Where(d => d.Role == "measured").ToList();
            var measuredTagIds = measuredDefs
                .Where(d => matched.ContainsKey(d.Key))
                .Select(d => matched[d.Key].TagId)
                .Distinct()
                .ToList();

            var samplesByTag = new Dictionary<long, List<(DateTimeOffset Time, double Value)>>();
            if (measuredTagIds.Count > 0)
            {
                var rows = await db.History1s.AsNoTracking()
                    .Where(h => measuredTagIds.Contains(h.TagId) && h.Time >= from && h.Time <= to)
                    .OrderBy(h => h.Time)
                    .Select(h => new { h.TagId, h.Time, h.Value })
                    .ToListAsync(cancellationToken);

                // Fallback 1s → 30s when empty.
                if (rows.Count == 0)
                {
                    rows = await db.History30s.AsNoTracking()
                        .Where(h => measuredTagIds.Contains(h.TagId) && h.Time >= from && h.Time <= to)
                        .OrderBy(h => h.Time)
                        .Select(h => new { h.TagId, h.Time, h.Value })
                        .ToListAsync(cancellationToken);
                }

                var bucketTicks = TimeSpan.FromMinutes(bucketMinutes).Ticks;
                foreach (var group in rows.GroupBy(x => x.TagId))
                {
                    // 1 điểm / 15 phút: lấy mẫu đầu tiên trong mỗi bucket UTC.
                    var points = group
                        .GroupBy(x => x.Time.UtcTicks / bucketTicks)
                        .Select(g =>
                        {
                            var first = g.OrderBy(p => p.Time).First();
                            var bucketStart = new DateTimeOffset(g.Key * bucketTicks, TimeSpan.Zero);
                            return (Time: bucketStart, Value: first.Value);
                        })
                        .OrderBy(p => p.Time)
                        .ToList();
                    samplesByTag[group.Key] = points;
                }

                interval = "15m";
            }

            var thresholdTagIds = seriesDefs
                .Where(d => d.Role == "threshold" && matched.ContainsKey(d.Key))
                .Select(d => matched[d.Key].TagId)
                .Distinct()
                .ToList();
            var thresholdValues = await LoadLatestHistoryAsync(thresholdTagIds, cancellationToken);

            var series = new List<StationChartSeriesDto>(seriesDefs.Count);
            foreach (var def in seriesDefs)
            {
                matched.TryGetValue(def.Key, out var tagMeta);
                var dto = new StationChartSeriesDto
                {
                    Key = def.Key,
                    Label = def.Label,
                    Role = def.Role,
                    TagId = tagMeta.TagId == 0 ? null : tagMeta.TagId,
                    TagCode = string.IsNullOrEmpty(tagMeta.Code) ? null : tagMeta.Code
                };

                if (def.Role == "threshold")
                {
                    double? value = null;
                    if (tagMeta.TagId != 0 && thresholdValues.TryGetValue(tagMeta.TagId, out var live))
                        value = live.Value;
                    else if (chart.Equals(StationChartCatalog.Temperature, StringComparison.OrdinalIgnoreCase))
                        value = 36.0 + Math.Abs(def.Key.GetHashCode() % 5) * 0.5;
                    else
                        value = 36.0 + Math.Abs(def.Key.GetHashCode() % 4);

                    dto.Points =
                    [
                        new StationChartPointDto { Timestamp = from, Value = value.Value },
                        new StationChartPointDto { Timestamp = to, Value = value.Value }
                    ];
                }
                else if (tagMeta.TagId != 0 && samplesByTag.TryGetValue(tagMeta.TagId, out var pts))
                {
                    dto.Points = pts
                        .Select(p => new StationChartPointDto { Timestamp = p.Time, Value = p.Value })
                        .ToList();
                }
                else
                {
                    dto.Points = [];
                }

                series.Add(dto);
            }

            return Result<StationChartHistoryDto>.Success(new StationChartHistoryDto
            {
                StationId = stationId,
                DeviceId = deviceId,
                Chart = chart.Trim().ToLowerInvariant(),
                Interval = interval,
                From = from,
                To = to,
                Series = series
            });
        });

    private static string NormalizeChartInterval(string? interval) =>
        (interval ?? "15m").Trim().ToLowerInvariant() switch
        {
            "15m" or "15min" => "15m",
            "30s" or "30sec" => "30s",
            "1m" or "1min" or "60s" => "1m",
            "1s" => "1s",
            _ => "15m"
        };

    private static bool TryResolveReportDayRange(
        DateOnly? reportDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        out DateTimeOffset from,
        out DateTimeOffset to,
        out (string Code, string Message)? error)
    {
        from = default;
        to = default;
        error = null;

        var vn = TimeSpan.FromHours(7);
        var todayVn = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(vn).DateTime);
        var date = reportDate ?? todayVn;
        var start = startTime ?? TimeOnly.MinValue;
        var end = endTime ?? new TimeOnly(23, 59, 59);

        from = new DateTimeOffset(date.ToDateTime(start), vn).ToUniversalTime();
        to = new DateTimeOffset(date.ToDateTime(end), vn).ToUniversalTime();

        if (from > to)
        {
            error = (ScadaErrorCodes.InvalidTimeRange, "startTime must be less than or equal to endTime.");
            return false;
        }

        return true;
    }

    private static bool TryResolveEventDateRange(
        DateOnly? fromDate,
        DateOnly? toDate,
        out DateTimeOffset from,
        out DateTimeOffset to,
        out (string Code, string Message)? error)
    {
        from = default;
        to = default;
        error = null;

        var vn = TimeSpan.FromHours(7);
        var todayVn = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(vn).DateTime);
        var startDay = fromDate ?? todayVn.AddDays(-30);
        var endDay = toDate ?? todayVn;

        from = new DateTimeOffset(startDay.ToDateTime(TimeOnly.MinValue), vn).ToUniversalTime();
        to = new DateTimeOffset(endDay.ToDateTime(new TimeOnly(23, 59, 59)), vn).ToUniversalTime();

        if (from > to)
        {
            error = (ScadaErrorCodes.InvalidTimeRange, "fromDate must be less than or equal to toDate.");
            return false;
        }

        return true;
    }

    private static string BuildEventTitle(
        string? description,
        string? deviceName,
        string? tagName,
        string type)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            var firstLine = description.Split('\n', 2)[0].Trim();
            if (firstLine.Length > 80) firstLine = firstLine[..80].Trim();
            if (firstLine.Length > 0) return firstLine;
        }

        var device = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim();
        if (type.Equals("ERROR", StringComparison.OrdinalIgnoreCase)
            || type.Equals("FAULT", StringComparison.OrdinalIgnoreCase)
            || type.Equals("ALARM", StringComparison.OrdinalIgnoreCase))
            return device is null ? "Lỗi thiết bị" : $"Lỗi {device}";

        if (type.Equals("WARNING", StringComparison.OrdinalIgnoreCase))
            return device is null ? "Cảnh báo" : $"Cảnh báo {device}";

        if (!string.IsNullOrWhiteSpace(tagName))
            return tagName.Trim();

        return device ?? type;
    }

    private static bool TryResolveWaterLevelRange(
        StationWaterLevelReportQuery query,
        out DateTimeOffset from,
        out DateTimeOffset to,
        out (string Code, string Message)? error) =>
        TryResolveReportDayRange(query.ReportDate, query.StartTime, query.EndTime, out from, out to, out error);

    private async Task<Dictionary<long, (double Value, DateTimeOffset Time)>> LoadLatestHistoryAsync(
        IReadOnlyList<long> tagIds,
        CancellationToken cancellationToken)
    {
        var latestByTagId = new Dictionary<long, (double Value, DateTimeOffset Time)>();
        if (tagIds.Count == 0)
            return latestByTagId;

        // Prefer Fake/Redis current-state store (simulator writes here).
        var live = await realtimeStore.GetManyAsync(tagIds, cancellationToken);
        foreach (var (tagId, rt) in live)
        {
            if (TryToDouble(rt.Value, out var number))
                latestByTagId[tagId] = (number, rt.Timestamp);
        }

        var missing = tagIds.Where(id => !latestByTagId.ContainsKey(id)).ToList();
        if (missing.Count == 0)
            return latestByTagId;

        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var samples = await db.History1s.AsNoTracking()
            .Where(h => missing.Contains(h.TagId) && h.Time >= from)
            .OrderByDescending(h => h.Time)
            .Select(h => new { h.TagId, h.Value, h.Time })
            .ToListAsync(cancellationToken);

        foreach (var sample in samples)
        {
            if (!latestByTagId.ContainsKey(sample.TagId))
                latestByTagId[sample.TagId] = (sample.Value, sample.Time);
        }

        return latestByTagId;
    }

    private static bool TryToDouble(object? value, out double number)
    {
        number = 0;
        switch (value)
        {
            case null:
                return false;
            case bool b:
                number = b ? 1 : 0;
                return true;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            case float or double or decimal:
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                number = parsed;
                return true;
            case System.Text.Json.JsonElement je:
                if (je.ValueKind == System.Text.Json.JsonValueKind.True) { number = 1; return true; }
                if (je.ValueKind == System.Text.Json.JsonValueKind.False) { number = 0; return true; }
                if (je.ValueKind == System.Text.Json.JsonValueKind.Number && je.TryGetDouble(out var jd))
                {
                    number = jd;
                    return true;
                }
                if (je.ValueKind == System.Text.Json.JsonValueKind.String
                    && double.TryParse(je.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var js))
                {
                    number = js;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static int? TryExtractBranchNumber(string name, string code)
    {
        var pumpCode = System.Text.RegularExpressions.Regex.Match(
            code ?? string.Empty, @"^Pump(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (pumpCode.Success && int.TryParse(pumpCode.Groups[1].Value, out var fromCode) && fromCode > 0)
            return fromCode;

        static int? Digits(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            // Prefer "Bơm 10" / "Pump 3" — not rated kW in the label.
            var labeled = System.Text.RegularExpressions.Regex.Match(
                text, @"(?:bơm|bom|pump)\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (labeled.Success && int.TryParse(labeled.Groups[1].Value, out var n) && n is > 0 and < 100)
                return n;
            var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)\s*$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var trailing) && trailing is > 0 and < 100)
                return trailing;
            return null;
        }

        return Digits(name) ?? Digits(code);
    }

    public async Task<Result<PaginationResult<PlcDto>>> GetPagedAsync(PlcQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.Plcs.AsNoTracking().AsQueryable();

        if (query.StationId is { } stationId)
            q = q.Where(p => p.StationId == stationId);
        if (query.IsEnable is { } isEnable)
            q = q.Where(p => p.IsEnable == isEnable);
        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(p => p.Code.Contains(keyword) || p.Name.Contains(keyword) || p.IpAddress.Contains(keyword));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(p => p.Code)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(p => new PlcDto
            {
                Id = p.Id,
                StationId = p.StationId,
                StationCode = p.Station.Code,
                Code = p.Code,
                Name = p.Name,
                PlcType = p.PlcType,
                IpAddress = p.IpAddress,
                Rack = p.Rack,
                Slot = p.Slot,
                Port = p.Port,
                PollingInterval = p.PollingInterval,
                ReconnectInterval = p.ReconnectInterval,
                Timeout = p.Timeout,
                MaxConnection = p.MaxConnection,
                IsEnable = p.IsEnable,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<PlcDto>>.Success(PaginationResult<PlcDto>.Create(items, total, query));
    }

    async Task<Result<PlcDto>> IPlcQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.Plcs.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PlcDto
            {
                Id = p.Id,
                StationId = p.StationId,
                StationCode = p.Station.Code,
                Code = p.Code,
                Name = p.Name,
                PlcType = p.PlcType,
                IpAddress = p.IpAddress,
                Rack = p.Rack,
                Slot = p.Slot,
                Port = p.Port,
                PollingInterval = p.PollingInterval,
                ReconnectInterval = p.ReconnectInterval,
                Timeout = p.Timeout,
                MaxConnection = p.MaxConnection,
                IsEnable = p.IsEnable,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<PlcDto>.Failure("Plc.NotFound", $"PLC '{id}' was not found.")
            : Result<PlcDto>.Success(dto);
    }

    public async Task<Result<PaginationResult<DeviceDto>>> GetPagedAsync(DeviceQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.Devices.AsNoTracking().AsQueryable();

        if (query.PlcId is { } plcId)
            q = q.Where(d => d.PlcId == plcId);
        if (!string.IsNullOrWhiteSpace(query.DeviceType))
            q = q.Where(d => d.DeviceType == query.DeviceType);
        if (query.IsEnable is { } isEnable)
            q = q.Where(d => d.IsActive == isEnable);
        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(d => d.Code.Contains(keyword) || d.Name.Contains(keyword) || d.DisplayName.Contains(keyword));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(d => d.Code)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(d => new DeviceDto
            {
                Id = d.Id,
                PlcId = d.PlcId,
                PlcCode = d.Plc.Code,
                Code = d.Code,
                Name = d.Name,
                DisplayName = d.DisplayName,
                DeviceType = d.DeviceType,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<DeviceDto>>.Success(PaginationResult<DeviceDto>.Create(items, total, query));
    }

    async Task<Result<DeviceDto>> IDeviceQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.Devices.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DeviceDto
            {
                Id = d.Id,
                PlcId = d.PlcId,
                PlcCode = d.Plc.Code,
                Code = d.Code,
                Name = d.Name,
                DisplayName = d.DisplayName,
                DeviceType = d.DeviceType,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<DeviceDto>.Failure("Device.NotFound", $"Device '{id}' was not found.")
            : Result<DeviceDto>.Success(dto);
    }

    public async Task<Result<PaginationResult<TagDto>>> GetPagedAsync(TagQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.Tags.AsNoTracking().AsQueryable();

        if (query.PlcId is { } plcId)
            q = q.Where(t => t.PlcId == plcId);
        if (query.DeviceId is { } deviceId)
            q = q.Where(t => t.DeviceId == deviceId);
        if (query.EnableRealtime is { } enableRealtime)
            q = q.Where(t => t.EnableRealtime == enableRealtime);
        if (query.EnableAlarm is { } enableAlarm)
            q = q.Where(t => t.EnableAlarm == enableAlarm);
        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(t =>
                t.Code.Contains(keyword) ||
                t.TagName.Contains(keyword) ||
                t.DisplayName.Contains(keyword) ||
                t.Address.Contains(keyword));
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(t => t.Code)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(t => new TagDto
            {
                Id = t.Id,
                PlcId = t.PlcId,
                DeviceId = t.DeviceId,
                PlcCode = t.Plc.Code,
                DeviceCode = t.Device.Code,
                Code = t.Code,
                TagName = t.TagName,
                DisplayName = t.DisplayName,
                Address = t.Address,
                DataType = t.DataType,
                Unit = t.Unit,
                Scale = t.Scale,
                OffsetValue = t.OffsetValue,
                ReadOnly = t.ReadOnly,
                WriteEnable = t.WriteEnable,
                EnableRealtime = t.EnableRealtime,
                EnableAlarm = t.EnableAlarm,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<TagDto>>.Success(PaginationResult<TagDto>.Create(items, total, query));
    }

    async Task<Result<TagDto>> ITagQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.Tags.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TagDto
            {
                Id = t.Id,
                PlcId = t.PlcId,
                DeviceId = t.DeviceId,
                PlcCode = t.Plc.Code,
                DeviceCode = t.Device.Code,
                Code = t.Code,
                TagName = t.TagName,
                DisplayName = t.DisplayName,
                Address = t.Address,
                DataType = t.DataType,
                Unit = t.Unit,
                Scale = t.Scale,
                OffsetValue = t.OffsetValue,
                ReadOnly = t.ReadOnly,
                WriteEnable = t.WriteEnable,
                EnableRealtime = t.EnableRealtime,
                EnableAlarm = t.EnableAlarm,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<TagDto>.Failure("Tag.NotFound", $"Tag '{id}' was not found.")
            : Result<TagDto>.Success(dto);
    }

    async Task<Result<IReadOnlyList<HistoryProfileDto>>> IHistoryProfileQueryService.GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await db.HistoryProfiles.AsNoTracking()
            .OrderBy(p => p.IntervalSecond)
            .Select(p => new HistoryProfileDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                IntervalSecond = p.IntervalSecond,
                RetentionDay = p.RetentionDay,
                CompressionDay = p.CompressionDay,
                Description = p.Description,
                IsEnable = p.IsEnable,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<HistoryProfileDto>>.Success(items);
    }

    async Task<Result<HistoryProfileDto>> IHistoryProfileQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.HistoryProfiles.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new HistoryProfileDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                IntervalSecond = p.IntervalSecond,
                RetentionDay = p.RetentionDay,
                CompressionDay = p.CompressionDay,
                Description = p.Description,
                IsEnable = p.IsEnable,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<HistoryProfileDto>.Failure("HistoryProfile.NotFound", $"History profile '{id}' was not found.")
            : Result<HistoryProfileDto>.Success(dto);
    }

    public async Task<Result<PaginationResult<TagHistoryConfigDto>>> GetPagedAsync(TagHistoryConfigQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.TagHistoryConfigs.AsNoTracking().AsQueryable();

        if (query.TagId is { } tagId)
            q = q.Where(c => c.TagId == tagId);
        if (query.HistoryProfileId is { } profileId)
            q = q.Where(c => c.HistoryProfileId == profileId);
        if (query.IsEnable is { } isEnable)
            q = q.Where(c => c.IsEnable == isEnable);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(c => c.Priority)
            .ThenBy(c => c.Id)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(c => new TagHistoryConfigDto
            {
                Id = c.Id,
                TagId = c.TagId,
                HistoryProfileId = c.HistoryProfileId,
                TagCode = c.Tag.Code,
                HistoryProfileCode = c.HistoryProfile.Code,
                Deadband = c.Deadband,
                Priority = c.Priority,
                Description = c.Description,
                IsEnable = c.IsEnable,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<TagHistoryConfigDto>>.Success(PaginationResult<TagHistoryConfigDto>.Create(items, total, query));
    }

    async Task<Result<TagHistoryConfigDto>> ITagHistoryConfigQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.TagHistoryConfigs.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new TagHistoryConfigDto
            {
                Id = c.Id,
                TagId = c.TagId,
                HistoryProfileId = c.HistoryProfileId,
                TagCode = c.Tag.Code,
                HistoryProfileCode = c.HistoryProfile.Code,
                Deadband = c.Deadband,
                Priority = c.Priority,
                Description = c.Description,
                IsEnable = c.IsEnable,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<TagHistoryConfigDto>.Failure("TagHistoryConfig.NotFound", $"Tag history config '{id}' was not found.")
            : Result<TagHistoryConfigDto>.Success(dto);
    }

    async Task<Result<IReadOnlyList<CommunicationConfigDto>>> ICommunicationConfigQueryService.GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await db.CommunicationConfigs.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new CommunicationConfigDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Protocol = c.Protocol,
                Description = c.Description,
                IsEnable = c.IsEnable,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CommunicationConfigDto>>.Success(items);
    }

    async Task<Result<CommunicationConfigDto>> ICommunicationConfigQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.CommunicationConfigs.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CommunicationConfigDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Protocol = c.Protocol,
                Description = c.Description,
                IsEnable = c.IsEnable,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<CommunicationConfigDto>.Failure("CommunicationConfig.NotFound", $"Communication config '{id}' was not found.")
            : Result<CommunicationConfigDto>.Success(dto);
    }

    async Task<Result<IReadOnlyList<MqttConfigDto>>> IMqttConfigQueryService.GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await db.MqttConfigs.AsNoTracking()
            .OrderBy(c => c.Code)
            .Select(c => new MqttConfigDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Broker = c.Broker,
                Port = c.Port,
                Username = c.Username,
                ClientId = c.ClientId,
                TopicPublish = c.TopicPublish,
                TopicSubscribe = c.TopicSubscribe,
                KeepAlive = c.KeepAlive,
                Qos = c.Qos,
                Retain = c.Retain,
                UseTls = c.UseTls,
                IsEnable = c.IsEnable,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<MqttConfigDto>>.Success(items);
    }

    async Task<Result<MqttConfigDto>> IMqttConfigQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.MqttConfigs.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new MqttConfigDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Broker = c.Broker,
                Port = c.Port,
                Username = c.Username,
                ClientId = c.ClientId,
                TopicPublish = c.TopicPublish,
                TopicSubscribe = c.TopicSubscribe,
                KeepAlive = c.KeepAlive,
                Qos = c.Qos,
                Retain = c.Retain,
                UseTls = c.UseTls,
                IsEnable = c.IsEnable,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<MqttConfigDto>.Failure("MqttConfig.NotFound", $"MQTT config '{id}' was not found.")
            : Result<MqttConfigDto>.Success(dto);
    }

    public async Task<Result<PaginationResult<ScadaUserDto>>> GetPagedAsync(ScadaUserQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.ScadaUsers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Role))
            q = q.Where(u => u.Role == query.Role);
        if (query.IsEnable is { } isEnable)
            q = q.Where(u => u.IsActive == isEnable);
        if (query.HasKeyword)
        {
            var keyword = query.Keyword!;
            q = q.Where(u =>
                u.Username.Contains(keyword)
                || u.FullName.Contains(keyword)
                || u.Role.Contains(keyword)
                || (u.Department != null && u.Department.Contains(keyword))
                || (u.Position != null && u.Position.Contains(keyword)));
        }

        var total = await q.CountAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var items = await q
            .OrderBy(u => u.Username)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Select(u => new ScadaUserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Department = u.Department,
                Position = u.Position,
                Role = u.Role,
                Level = u.Level,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                LockoutUntil = u.LockoutUntil,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Status =
                    !u.IsActive
                        ? "Ngưng hoạt động"
                        : (u.LockoutUntil != null && u.LockoutUntil > now)
                            ? "Bị khóa (đăng nhập sai)"
                            : u.MustChangePassword
                                ? "Bị khóa (hết hạn mật khẩu)"
                                : "Đang hoạt động"
            })
            .ToListAsync(cancellationToken);

        return Result<PaginationResult<ScadaUserDto>>.Success(PaginationResult<ScadaUserDto>.Create(items, total, query));
    }

    async Task<Result<ScadaUserDto>> IScadaUserQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var dto = await db.ScadaUsers.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new ScadaUserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Department = u.Department,
                Position = u.Position,
                Role = u.Role,
                Level = u.Level,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                LockoutUntil = u.LockoutUntil,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Status =
                    !u.IsActive
                        ? "Ngưng hoạt động"
                        : (u.LockoutUntil != null && u.LockoutUntil > now)
                            ? "Bị khóa (đăng nhập sai)"
                            : u.MustChangePassword
                                ? "Bị khóa (hết hạn mật khẩu)"
                                : "Đang hoạt động"
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<ScadaUserDto>.Failure("ScadaUser.NotFound", $"SCADA user '{id}' was not found.")
            : Result<ScadaUserDto>.Success(dto);
    }

    public Task<Result<ScadaUserDto>> CreateAsync(
        CreateScadaUserRequest request,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var username = (request.Username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(username))
                return Result<ScadaUserDto>.Failure("Auth.Validation", "Username is required.");

            if (username.Length > 32)
                return Result<ScadaUserDto>.Failure("Auth.Validation", "Username must be at most 32 characters.");

            if (!string.IsNullOrEmpty(request.ConfirmPassword)
                && !string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                return Result<ScadaUserDto>.Failure("Auth.Validation", "Confirm password does not match.");

            if (!TryValidatePassword(request.Password, out var passwordError))
                return Result<ScadaUserDto>.Failure("Auth.PasswordPolicy", passwordError);

            var fullName = (request.FullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = username;

            var role = NormalizeScadaRole(request.Role);
            if (role is null)
                return Result<ScadaUserDto>.Failure(
                    "Auth.Validation",
                    "Role must be viewer, Operator, or Administrator.");

            if (request.Level is { } level && (level < 1 || level > 100))
                return Result<ScadaUserDto>.Failure("Auth.Validation", "Level must be between 1 and 100.");

            var exists = await db.ScadaUsers.AnyAsync(u => u.Username == username, cancellationToken);
            if (exists)
                return Result<ScadaUserDto>.Failure("Auth.UsernameTaken", "Username is already taken.");

            var now = DateTimeOffset.UtcNow;
            var actor = currentUser.Username;
            var user = new ScadaUser
            {
                Username = username,
                FullName = fullName,
                PasswordHash = passwordHasher.Hash(request.Password),
                Role = role,
                IsActive = request.IsActive,
                MustChangePassword = request.MustChangePassword,
                Department = NullIfWhiteSpace(request.Department),
                Position = NullIfWhiteSpace(request.Position),
                Unit = NullIfWhiteSpace(request.Unit) ?? NullIfWhiteSpace(request.Department),
                Description = NullIfWhiteSpace(request.Description),
                Level = request.Level,
                PasswordUpdatedAt = request.MustChangePassword ? null : now,
                CreatedBy = actor,
                UpdatedBy = actor,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.ScadaUsers.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            await systemAudit.LogAsync(new SystemAuditEntry
            {
                Action = AuditActionNames.CreateUser,
                EventType = AuditEventType.Configuration,
                Status = AuditStatus.Success,
                Module = "ScadaUsers",
                EntityType = "ScadaUser",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                Description = $"Created SCADA user '{user.Username}'.",
                UserId = currentUser.OperatorUserId,
                UserName = actor
            }, cancellationToken);

            return Result<ScadaUserDto>.Success(MapScadaUserDto(user, now));
        });

    public Task<Result<ScadaUserDto>> UpdateAsync(
        long id,
        UpdateScadaUserRequest request,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var user = await db.ScadaUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user is null)
                return Result<ScadaUserDto>.Failure("ScadaUser.NotFound", $"SCADA user '{id}' was not found.");

            if (request.FullName is not null)
            {
                var fullName = request.FullName.Trim();
                if (fullName.Length == 0)
                    return Result<ScadaUserDto>.Failure("Auth.Validation", "Full name cannot be empty.");
                user.FullName = fullName;
            }

            if (request.Email is not null)
            {
                var email = NullIfWhiteSpace(request.Email);
                if (email is not null)
                {
                    var emailTaken = await db.ScadaUsers.AnyAsync(
                        u => u.Id != id && u.Email == email, cancellationToken);
                    if (emailTaken)
                        return Result<ScadaUserDto>.Failure("Auth.Conflict", "Email is already in use.");
                }
                user.Email = email;
            }

            if (request.Role is not null)
            {
                var role = NormalizeScadaRole(request.Role);
                if (role is null)
                    return Result<ScadaUserDto>.Failure(
                        "Auth.Validation",
                        "Role must be viewer, Operator, or Administrator.");
                user.Role = role;
            }

            if (request.Level is { } level)
            {
                if (level < 1 || level > 100)
                    return Result<ScadaUserDto>.Failure("Auth.Validation", "Level must be between 1 and 100.");
                user.Level = level;
            }

            if (request.Department is not null)
                user.Department = NullIfWhiteSpace(request.Department);
            if (request.Position is not null)
                user.Position = NullIfWhiteSpace(request.Position);
            if (request.Unit is not null)
                user.Unit = NullIfWhiteSpace(request.Unit);
            if (request.Description is not null)
                user.Description = NullIfWhiteSpace(request.Description);
            if (request.IsActive is { } isActive)
                user.IsActive = isActive;
            if (request.MustChangePassword is { } mustChange)
                user.MustChangePassword = mustChange;

            var now = DateTimeOffset.UtcNow;
            user.UpdatedAt = now;
            user.UpdatedBy = currentUser.Username;

            await db.SaveChangesAsync(cancellationToken);

            await systemAudit.LogAsync(new SystemAuditEntry
            {
                Action = AuditActionNames.UpdateUser,
                EventType = AuditEventType.Configuration,
                Status = AuditStatus.Success,
                Module = "ScadaUsers",
                EntityType = "ScadaUser",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                Description = $"Updated SCADA user '{user.Username}'.",
                UserId = currentUser.OperatorUserId,
                UserName = currentUser.Username
            }, cancellationToken);

            return Result<ScadaUserDto>.Success(MapScadaUserDto(user, now));
        });

    public Task<Result> DeactivateAsync(long id, CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var user = await db.ScadaUsers.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            if (user is null)
                return Result.Failure("ScadaUser.NotFound", $"SCADA user '{id}' was not found.");

            if (currentUser.OperatorUserId is { } selfId && selfId == id)
                return Result.Failure("Auth.Forbidden", "Cannot deactivate your own account.");

            if (!user.IsActive)
                return Result.Success();

            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            user.UpdatedBy = currentUser.Username;
            await db.SaveChangesAsync(cancellationToken);

            await systemAudit.LogAsync(new SystemAuditEntry
            {
                Action = AuditActionNames.DeactivateUser,
                EventType = AuditEventType.Configuration,
                Status = AuditStatus.Success,
                Module = "ScadaUsers",
                EntityType = "ScadaUser",
                EntityId = user.Id.ToString(CultureInfo.InvariantCulture),
                Description = $"Deactivated SCADA user '{user.Username}'.",
                UserId = currentUser.OperatorUserId,
                UserName = currentUser.Username
            }, cancellationToken);

            return Result.Success();
        });

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeScadaRole(string? raw)
    {
        var role = (raw ?? string.Empty).Trim();
        if (role.Length == 0) return ScadaRoles.Operator;
        if (role.Equals("viewer", StringComparison.OrdinalIgnoreCase))
            return ScadaRoles.Viewer;
        if (role.Equals("operator", StringComparison.OrdinalIgnoreCase))
            return ScadaRoles.Operator;
        if (role.Equals("admin", StringComparison.OrdinalIgnoreCase)
            || role.Equals("administrator", StringComparison.OrdinalIgnoreCase))
            return ScadaRoles.Admin;
        return null;
    }

    private bool TryValidatePassword(string? password, out string error)
    {
        var opt = passwordOptions.Value;
        return PasswordComplexity.TryValidate(
            password,
            opt.MinLength > 0 ? opt.MinLength : PasswordComplexity.DefaultMinLength,
            opt.MaxLength > 0 ? opt.MaxLength : PasswordComplexity.DefaultMaxLength,
            opt.RequireUppercase,
            opt.RequireLowercase,
            opt.RequireDigit,
            opt.RequireSpecial,
            out error);
    }

    private static ScadaUserDto MapScadaUserDto(ScadaUser u, DateTimeOffset now) =>
        new()
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            Department = u.Department,
            Position = u.Position,
            Role = u.Role,
            Level = u.Level,
            IsActive = u.IsActive,
            MustChangePassword = u.MustChangePassword,
            LockoutUntil = u.LockoutUntil,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            Status =
                !u.IsActive
                    ? "Ngưng hoạt động"
                    : (u.LockoutUntil != null && u.LockoutUntil > now)
                        ? "Bị khóa (đăng nhập sai)"
                        : u.MustChangePassword
                            ? "Bị khóa (hết hạn mật khẩu)"
                            : "Đang hoạt động"
        };

    async Task<Result<IReadOnlyList<AppSettingDto>>> IAppSettingQueryService.GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await db.AppSettings.AsNoTracking()
            .OrderBy(s => s.SettingKey)
            .Select(s => new AppSettingDto
            {
                Id = s.Id,
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                DataType = s.DataType,
                Description = s.Description,
                IsEnable = s.IsEnable,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AppSettingDto>>.Success(items);
    }

    async Task<Result<AppSettingDto>> IAppSettingQueryService.GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var dto = await db.AppSettings.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new AppSettingDto
            {
                Id = s.Id,
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                DataType = s.DataType,
                Description = s.Description,
                IsEnable = s.IsEnable,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<AppSettingDto>.Failure("AppSetting.NotFound", $"App setting '{id}' was not found.")
            : Result<AppSettingDto>.Success(dto);
    }

    public async Task<Result<AppSettingDto>> GetByKeyAsync(string settingKey, CancellationToken cancellationToken = default)
    {
        var dto = await db.AppSettings.AsNoTracking()
            .Where(s => s.SettingKey == settingKey)
            .Select(s => new AppSettingDto
            {
                Id = s.Id,
                SettingKey = s.SettingKey,
                SettingValue = s.SettingValue,
                DataType = s.DataType,
                Description = s.Description,
                IsEnable = s.IsEnable,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result<AppSettingDto>.Failure("AppSetting.NotFound", $"App setting key '{settingKey}' was not found.")
            : Result<AppSettingDto>.Success(dto);
    }

    public Task<Result<IReadOnlyList<AppSettingCatalogItemDto>>> GetCatalogAsync(
        CancellationToken cancellationToken = default) =>
        SafeAsync(() =>
        {
            var items = AppSettingCatalog.All
                .Select(d => new AppSettingCatalogItemDto
                {
                    Key = d.Key,
                    DataType = d.DataType,
                    Description = d.Description,
                    Source = d.Source.ToString(),
                    EditableViaApi = d.EditableViaApi,
                    DefaultValue = d.DefaultValue,
                    Min = d.Min,
                    Max = d.Max
                })
                .ToList();
            return Task.FromResult(Result<IReadOnlyList<AppSettingCatalogItemDto>>.Success(items));
        });

    public Task<Result<AppSettingDto>> UpdateByKeyAsync(
        string settingKey,
        UpdateAppSettingRequest request,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            if (!AppSettingCatalog.TryValidateEditableValue(settingKey, request.SettingValue, out var error))
                return Result<AppSettingDto>.Failure("ValidationError", error);

            // Dedicated session endpoint remains preferred; this path keeps one allowlisted writer.
            if (string.Equals(settingKey, AppSettingCatalog.SessionIdleTimeoutMinutes, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(request.SettingValue, out var minutes))
            {
                var sessionResult = await UpdateSessionPolicyAsync(
                    new UpdateSessionPolicyRequest { IdleTimeoutMinutes = minutes },
                    cancellationToken);
                if (sessionResult.IsFailure)
                    return Result<AppSettingDto>.Failure(sessionResult.ErrorCode, sessionResult.Errors);

                return await GetByKeyAsync(settingKey, cancellationToken);
            }

            var now = DateTimeOffset.UtcNow;
            var row = await db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey, cancellationToken);

            if (row is null)
            {
                if (!AppSettingCatalog.TryGet(settingKey, out var def))
                    return Result<AppSettingDto>.Failure("AppSetting.NotFound", $"Unknown setting '{settingKey}'.");

                row = new AppSetting
                {
                    SettingKey = def.Key,
                    SettingValue = request.SettingValue,
                    DataType = def.DataType,
                    Description = def.Description,
                    IsEnable = request.IsEnable ?? true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.AppSettings.Add(row);
            }
            else
            {
                row.SettingValue = request.SettingValue;
                if (request.IsEnable is { } enabled)
                    row.IsEnable = enabled;
                row.UpdatedAt = now;
            }

            await db.SaveChangesAsync(cancellationToken);

            await systemAudit.LogAsync(new SystemAuditEntry
            {
                Action = AuditActionNames.ConfigurationChanged,
                EventType = AuditEventType.Configuration,
                Status = AuditStatus.Success,
                Module = "AppSettings",
                EntityType = "AppSetting",
                EntityId = settingKey,
                Description = $"Updated app setting '{settingKey}'.",
                UserId = currentUser.OperatorUserId,
                UserName = currentUser.Username
            }, cancellationToken);

            return Result<AppSettingDto>.Success(new AppSettingDto
            {
                Id = row.Id,
                SettingKey = row.SettingKey,
                SettingValue = row.SettingValue,
                DataType = row.DataType,
                Description = row.Description,
                IsEnable = row.IsEnable,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt
            });
        });

    public Task<Result<SessionPolicyDto>> GetSessionPolicyAsync(
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var row = await db.AppSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == SessionIdleTimeoutPolicy.SettingKey, cancellationToken);

            var parsedOk = SessionIdleTimeoutPolicy.TryParseMinutes(row?.SettingValue, out var minutes);
            return Result<SessionPolicyDto>.Success(new SessionPolicyDto
            {
                SettingKey = SessionIdleTimeoutPolicy.SettingKey,
                IdleTimeoutMinutes = minutes,
                IsDefault = row is null || !parsedOk,
                UpdatedAt = row?.UpdatedAt
            });
        });

    public Task<Result<SessionPolicyDto>> UpdateSessionPolicyAsync(
        UpdateSessionPolicyRequest request,
        CancellationToken cancellationToken = default) =>
        SafeAsync(async () =>
        {
            var minutes = request.IdleTimeoutMinutes;
            if (minutes < SessionIdleTimeoutPolicy.MinMinutes
                || minutes > SessionIdleTimeoutPolicy.MaxMinutes)
            {
                return Result<SessionPolicyDto>.Failure(
                    "ValidationError",
                    $"Idle timeout must be between {SessionIdleTimeoutPolicy.MinMinutes} and {SessionIdleTimeoutPolicy.MaxMinutes} minutes.");
            }

            var now = DateTimeOffset.UtcNow;
            var row = await db.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == SessionIdleTimeoutPolicy.SettingKey, cancellationToken);

            if (row is null)
            {
                row = new AppSetting
                {
                    SettingKey = SessionIdleTimeoutPolicy.SettingKey,
                    SettingValue = minutes.ToString(CultureInfo.InvariantCulture),
                    DataType = "int",
                    Description = "FE idle timeout (minutes)",
                    IsEnable = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.AppSettings.Add(row);
            }
            else
            {
                row.SettingValue = minutes.ToString(CultureInfo.InvariantCulture);
                row.DataType = "int";
                row.IsEnable = true;
                row.UpdatedAt = now;
                if (string.IsNullOrWhiteSpace(row.Description))
                    row.Description = "FE idle timeout (minutes)";
            }

            await db.SaveChangesAsync(cancellationToken);

            await systemAudit.LogAsync(new SystemAuditEntry
            {
                Action = AuditActionNames.ConfigurationChanged,
                EventType = AuditEventType.Configuration,
                Status = AuditStatus.Success,
                Module = "SessionPolicy",
                EntityType = "AppSetting",
                EntityId = SessionIdleTimeoutPolicy.SettingKey,
                Description = $"Updated idle timeout to {minutes} minutes.",
                UserId = currentUser.OperatorUserId,
                UserName = currentUser.Username
            }, cancellationToken);

            return Result<SessionPolicyDto>.Success(new SessionPolicyDto
            {
                SettingKey = SessionIdleTimeoutPolicy.SettingKey,
                IdleTimeoutMinutes = minutes,
                IsDefault = false,
                UpdatedAt = row.UpdatedAt
            });
        });
}
