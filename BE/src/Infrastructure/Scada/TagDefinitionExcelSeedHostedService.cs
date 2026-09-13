using Backend.Application.Options;
using Backend.Application.Scada;
using Backend.Domain.Entities.Scada;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence.Context;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Scada;

/// <summary>
/// Idempotent Excel → DB seed for Station/PLC/Device/Tag + screen mapping.
/// Excel is metadata only; matching is Station + PLC + Device + Tag.
/// </summary>
public sealed class TagDefinitionExcelSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IOptions<RealtimeOptions> options,
    ILogger<TagDefinitionExcelSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var cfg = options.Value;
        if (!cfg.SeedExcelOnStartup)
            return;

        var path = ResolveExcelPath(cfg.ExcelPath, environment);
        if (path is null)
        {
            logger.LogWarning("Tag definition Excel not found at {Path}; skip screen mapping seed", cfg.ExcelPath);
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var imported = ImportWorkbook(path);
            if (imported.Count == 0)
            {
                logger.LogWarning("Tag definition Excel {Path} produced no rows", path);
                return;
            }

        var now = DateTimeOffset.UtcNow;
        var counts = new Dictionary<ScadaScreenType, int>();
        var createdTags = 0;
        var mappings = 0;
        var nextTagId = (await db.Tags.MaxAsync(t => (long?)t.Id, cancellationToken) ?? 0) + 1;

        foreach (var group in imported.GroupBy(r => (r.StationCode, r.PlcCode)))
        {
            var station = await ResolveStationAsync(db, group.Key.StationCode, cancellationToken);
            if (station is null)
            {
                logger.LogWarning("Skip Excel group: station {Station} not found", group.Key.StationCode);
                continue;
            }

            var plcId = await ResolvePlcIdAsync(db, station, group.Key.PlcCode, now, cancellationToken);
            if (plcId is null)
            {
                logger.LogWarning("Skip Excel group: no PLC for station {Station}", station.Code);
                continue;
            }

            foreach (var deviceGroup in group.GroupBy(r => r.DeviceName, StringComparer.OrdinalIgnoreCase))
            {
                var device = await ResolveDeviceAsync(db, station, plcId.Value, deviceGroup.Key, now, cancellationToken);

                foreach (var row in deviceGroup)
                {
                    var tag = await ResolveTagAsync(db, plcId.Value, device, row, now, nextTagId, cancellationToken);
                    if (tag.Id == nextTagId)
                    {
                        db.Tags.Add(tag);
                        await db.SaveChangesAsync(cancellationToken);
                        createdTags++;
                        nextTagId++;
                    }

                    foreach (var map in row.Screens)
                    {
                        var existing = await db.TagScreenMappings
                            .FirstOrDefaultAsync(m => m.TagId == tag.Id && m.ScreenType == map.Screen, cancellationToken);
                        if (existing is null)
                        {
                            db.TagScreenMappings.Add(new TagScreenMapping
                            {
                                TagId = tag.Id,
                                ScreenType = map.Screen,
                                IsRealtime = ScadaScreenMapping.IsRealtime(map.Screen),
                                MappingLabel = map.Label,
                                CreatedAt = now,
                                UpdatedAt = now
                            });
                            mappings++;
                        }
                        else if (!string.Equals(existing.MappingLabel, map.Label, StringComparison.Ordinal)
                                 || existing.IsRealtime != ScadaScreenMapping.IsRealtime(map.Screen))
                        {
                            existing.MappingLabel = map.Label;
                            existing.IsRealtime = ScadaScreenMapping.IsRealtime(map.Screen);
                            existing.UpdatedAt = now;
                        }

                        counts[map.Screen] = counts.GetValueOrDefault(map.Screen) + 1;
                    }

                    var realtime = row.Screens.Any(s => ScadaScreenMapping.IsRealtime(s.Screen));
                    var alarm = row.Screens.Any(s => s.Screen == ScadaScreenType.Loi);
                    if (tag.EnableRealtime != realtime || tag.EnableAlarm != alarm)
                    {
                        tag.EnableRealtime = realtime;
                        tag.EnableAlarm = alarm;
                        tag.UpdatedAt = now;
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Excel tag mapping seed done. Tags+={Tags} MappingInserts={Maps} Counts={Counts}",
            createdTags, mappings,
            string.Join(", ", counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tag definition Excel seed failed; API will continue");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<Station?> ResolveStationAsync(ApplicationDbContext db, string? excelStation, CancellationToken ct)
    {
        var code = (excelStation ?? string.Empty).Trim();
        var stations = await db.Stations.ToListAsync(ct);
        return stations.FirstOrDefault(s =>
                   string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(s.Code, "TB01", StringComparison.OrdinalIgnoreCase) && code is "TBAB" or "TB01")
               ?? stations.FirstOrDefault(s =>
                   s.Name.Contains("Ấp Bắc", StringComparison.OrdinalIgnoreCase)
                   || s.Name.Contains("Ap Bac", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<long?> ResolvePlcIdAsync(
        ApplicationDbContext db, Station station, string? excelPlc, DateTimeOffset now, CancellationToken ct)
    {
        var code = string.IsNullOrWhiteSpace(excelPlc) ? "PLC01" : excelPlc.Trim();
        var existingId = await db.Plcs
            .Where(p => p.StationId == station.Id && p.Code == code)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(ct);
        if (existingId is not null)
            return existingId;

        existingId = await db.Plcs
            .Where(p => p.StationId == station.Id)
            .OrderBy(p => p.Id)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(ct);
        if (existingId is not null && string.Equals(code, "PLC01", StringComparison.OrdinalIgnoreCase))
            return existingId;

        db.Plcs.Add(new Plc
        {
            StationId = station.Id,
            Code = code,
            Name = code,
            PlcType = "S7",
            IpAddress = "127.0.0.1",
            Port = 102,
            PollingInterval = 1000,
            ReconnectInterval = 5000,
            Timeout = 3000,
            MaxConnection = 1,
            IsEnable = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);

        return await db.Plcs
            .Where(p => p.StationId == station.Id && p.Code == code)
            .Select(p => (long?)p.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static async Task<Device> ResolveDeviceAsync(
        ApplicationDbContext db, Station station, long plcId, string excelDevice, DateTimeOffset now, CancellationToken ct)
    {
        var name = excelDevice.Trim();
        var code = $"{station.Code}_{name}";
        var existing = await db.Devices.FirstOrDefaultAsync(d => d.Code == code, ct)
                       ?? await db.Devices.FirstOrDefaultAsync(
                           d => d.PlcId == plcId && (d.Name == name || d.DisplayName == name || d.Code == name), ct);
        if (existing is not null)
            return existing;

        var device = new Device
        {
            PlcId = plcId,
            Code = code,
            Name = name,
            DisplayName = name,
            DeviceType = InferDeviceType(name),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Devices.Add(device);
        await db.SaveChangesAsync(ct);
        return device;
    }

    private static async Task<Tag> ResolveTagAsync(
        ApplicationDbContext db, long plcId, Device device, ExcelTagRow row, DateTimeOffset now, long nextTagId, CancellationToken ct)
    {
        var code = row.FullTag.Trim();
        var existing = await db.Tags.FirstOrDefaultAsync(t => t.Code == code, ct)
                       ?? await db.Tags.FirstOrDefaultAsync(
                           t => t.DeviceId == device.Id && (t.TagName == code || t.Code == row.ShortTag), ct);
        if (existing is not null)
        {
            existing.Address = string.IsNullOrWhiteSpace(row.Address) ? existing.Address : row.Address;
            existing.DataType = string.IsNullOrWhiteSpace(row.DataType) ? existing.DataType : row.DataType;
            if (!string.IsNullOrWhiteSpace(row.Description))
            {
                existing.Description = row.Description;
                existing.DisplayName = row.Description;
            }
            existing.PlcId = plcId;
            existing.DeviceId = device.Id;
            existing.UpdatedAt = now;
            return existing;
        }

        return new Tag
        {
            Id = nextTagId,
            PlcId = plcId,
            DeviceId = device.Id,
            Code = code,
            TagName = code,
            DisplayName = string.IsNullOrWhiteSpace(row.Description) ? code : row.Description,
            Address = row.Address ?? string.Empty,
            DataType = string.IsNullOrWhiteSpace(row.DataType) ? "real" : row.DataType,
            ReadOnly = true,
            WriteEnable = false,
            EnableRealtime = row.Screens.Any(s => ScadaScreenMapping.IsRealtime(s.Screen)),
            EnableAlarm = row.Screens.Any(s => s.Screen == ScadaScreenType.Loi),
            Description = row.Description,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string InferDeviceType(string name)
    {
        if (name.StartsWith("Pump", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Priming", StringComparison.OrdinalIgnoreCase))
            return "Pump";
        if (name.StartsWith("Meter", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("Metter", StringComparison.OrdinalIgnoreCase))
            return "PowerMeter";
        if (name.Contains("Level", StringComparison.OrdinalIgnoreCase))
            return "Level";
        return "Other";
    }

    private static string? ResolveExcelPath(string configured, IHostEnvironment env)
    {
        var candidates = new[]
        {
            Path.IsPathRooted(configured) ? configured : Path.Combine(env.ContentRootPath, configured),
            Path.Combine(AppContext.BaseDirectory, configured),
            Path.Combine(env.ContentRootPath, "..", "..", "data", Path.GetFileName(configured)),
            Path.Combine(env.ContentRootPath, "..", "..", "..", "..", "data", Path.GetFileName(configured))
        };
        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    internal static List<ExcelTagRow> ImportWorkbook(string path)
    {
        using var workbook = new XLWorkbook(path);
        var ws = workbook.Worksheets.FirstOrDefault(w =>
                     w.Name.Contains("Trung", StringComparison.OrdinalIgnoreCase))
                 ?? workbook.Worksheet(2);

        ExpandMergedValues(ws);

        var rows = new List<ExcelTagRow>();
        string? station = "TBAB";
        string? plc = "PLC01";
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

        for (var r = 4; r <= lastRow; r++)
        {
            var fullTag = Cell(ws, r, 2);
            if (string.IsNullOrWhiteSpace(fullTag))
                continue;

            var stationCell = Cell(ws, r, 7);
            var plcCell = Cell(ws, r, 8);
            if (!string.IsNullOrWhiteSpace(stationCell)) station = stationCell;
            if (!string.IsNullOrWhiteSpace(plcCell)) plc = plcCell;

            var screens = new List<(ScadaScreenType Screen, string Label)>();
            TryAddScreen(screens, ScadaScreenType.NguyenLy, Cell(ws, r, 13));
            TryAddScreen(screens, ScadaScreenType.CongNghe, Cell(ws, r, 14));
            TryAddScreen(screens, ScadaScreenType.ChiTietBom, Cell(ws, r, 15));
            TryAddScreen(screens, ScadaScreenType.Loi, Cell(ws, r, 16));
            TryAddScreen(screens, ScadaScreenType.Trend, Cell(ws, r, 17));
            TryAddScreen(screens, ScadaScreenType.BaoCao, Cell(ws, r, 18));

            rows.Add(new ExcelTagRow
            {
                StationCode = station,
                PlcCode = plc,
                DeviceName = Cell(ws, r, 9) ?? "Unknown",
                ShortTag = Cell(ws, r, 10) ?? string.Empty,
                FullTag = fullTag.Trim(),
                Address = Cell(ws, r, 3) ?? string.Empty,
                DataType = Cell(ws, r, 4) ?? string.Empty,
                Description = Cell(ws, r, 5),
                Screens = screens
            });
        }

        return rows;
    }

    private static void TryAddScreen(List<(ScadaScreenType Screen, string Label)> screens, ScadaScreenType type, string? cell)
    {
        if (!ScadaScreenMapping.IsMappedCell(type, cell))
            return;
        screens.Add((type, cell!.Trim()));
    }

    private static void ExpandMergedValues(IXLWorksheet ws)
    {
        foreach (var range in ws.MergedRanges.ToList())
        {
            var value = range.FirstCell().GetFormattedString();
            if (string.IsNullOrWhiteSpace(value))
                continue;
            foreach (var cell in range.Cells())
            {
                if (string.IsNullOrWhiteSpace(cell.GetFormattedString()))
                    cell.SetValue(value);
            }
        }
    }

    private static string? Cell(IXLWorksheet ws, int row, int col)
    {
        var text = ws.Cell(row, col).GetFormattedString()?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    internal sealed class ExcelTagRow
    {
        public string? StationCode { get; init; }
        public string? PlcCode { get; init; }
        public string DeviceName { get; init; } = string.Empty;
        public string ShortTag { get; init; } = string.Empty;
        public string FullTag { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string DataType { get; init; } = string.Empty;
        public string? Description { get; init; }
        public List<(ScadaScreenType Screen, string Label)> Screens { get; init; } = [];
    }
}
