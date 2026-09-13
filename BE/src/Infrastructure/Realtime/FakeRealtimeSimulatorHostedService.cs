using Backend.Application.Options;
using Backend.Application.Realtime;
using Backend.Application.Scada;
using Backend.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Realtime;

/// <summary>
/// Fake PLC simulator: advances coherent pump physics, writes last-values to
/// <see cref="IRealtimeDataStore"/> (Fake or Redis), and publishes SignalR deltas.
/// Tag catalog comes from DB mappings — no hard-coded station/device lists.
/// </summary>
public sealed class FakeRealtimeSimulatorHostedService(
    IServiceScopeFactory scopeFactory,
    IRealtimeDataStore store,
    IScadaRealtimeBroadcaster broadcaster,
    PumpSimulationStateStore pumpStates,
    IOptions<RealtimeOptions> options,
    ILogger<FakeRealtimeSimulatorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = options.Value;
        if (!cfg.SimulateChanges)
            return;

        var delay = TimeSpan.FromSeconds(Math.Max(1, cfg.SimulateIntervalSeconds));
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Fake realtime simulator tick failed");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rows = await (
            from m in db.TagScreenMappings.AsNoTracking()
            join t in db.Tags.AsNoTracking() on m.TagId equals t.Id
            join d in db.Devices.AsNoTracking() on t.DeviceId equals d.Id
            join p in db.Plcs.AsNoTracking() on t.PlcId equals p.Id
            where m.IsRealtime
            select new SimTagRow(
                t.Id,
                t.DeviceId,
                p.StationId,
                p.Code,
                d.Code,
                d.DeviceType,
                t.Code,
                t.TagName,
                t.DataType,
                m.ScreenType)
        ).ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return;

        var byDevice = rows.GroupBy(r => r.DeviceId).ToList();
        var rng = Random.Shared;
        var now = DateTimeOffset.UtcNow;

        // Prefer pump devices; always include Level + Meter/Metter so UI electrical/water stay live.
        var pumpDevices = byDevice.Where(g => IsPump(g.First().DeviceType, g.First().DeviceCode)).ToList();
        var auxDevices = byDevice.Where(g =>
        {
            var code = g.First().DeviceCode ?? string.Empty;
            var type = g.First().DeviceType ?? string.Empty;
            return IsAuxRealtimeDevice(type, code);
        }).ToList();
        var otherDevices = byDevice
            .Where(g => !IsPump(g.First().DeviceType, g.First().DeviceCode)
                        && !IsAuxRealtimeDevice(g.First().DeviceType, g.First().DeviceCode))
            .ToList();

        var selected = pumpDevices
            .Concat(auxDevices)
            .Concat(otherDevices.OrderBy(_ => rng.Next()).Take(Math.Min(3, otherDevices.Count)))
            .GroupBy(g => g.Key)
            .Select(g => g.First())
            .ToList();

        foreach (var deviceGroup in selected)
        {
            var sample = deviceGroup.First();
            var isPump = IsPump(sample.DeviceType, sample.DeviceCode);
            var state = pumpStates.GetOrAdd(sample.DeviceId);
            if (isPump)
                PumpPhysicsSimulator.Tick(state, rng);

            var screenSlugs = deviceGroup
                .Select(g => ScadaScreenMapping.ToSlug(g.ScreenType))
                .Distinct()
                .ToList();

            // Live station UIs (process / schematic / devices) always receive pump/aux deltas
            // even when Excel mapping only lists the tag on one screen.
            foreach (var extra in new[] { "cong-nghe", "nguyen-ly", "chi-tiet-bom" })
            {
                if (!screenSlugs.Contains(extra, StringComparer.Ordinal))
                    screenSlugs.Add(extra);
            }

            // One row per tag id (a tag may map to multiple screens).
            foreach (var tagGroup in deviceGroup.GroupBy(t => t.TagId))
            {
                var tag = tagGroup.First();
                object? value;
                string quality;

                if (isPump)
                {
                    var kind = PumpTagClassifier.Classify(tag.Code, tag.TagName);
                    (value, quality) = PumpPhysicsSimulator.ResolveTag(kind, tag.DataType, state, rng, tag.TagId);
                }
                else
                {
                    // Meters / Level: continuous analog motion in realistic bands.
                    var kind = PumpTagClassifier.Classify(tag.Code, tag.TagName);
                    value = kind switch
                    {
                        PumpTagClassifier.Kind.Voltage =>
                            RealtimeValueGenerator.GenerateAnalog(tag.TagId, 380, 18, 2.0),
                        PumpTagClassifier.Kind.Current =>
                            RealtimeValueGenerator.GenerateAnalog(tag.TagId, 110, 55, 1.6),
                        PumpTagClassifier.Kind.Frequency =>
                            RealtimeValueGenerator.GenerateAnalog(tag.TagId, 50, 0.9, 2.2),
                        PumpTagClassifier.Kind.PowerFactor =>
                            Math.Clamp(RealtimeValueGenerator.GenerateAnalog(tag.TagId, 0.86, 0.1, 2.8), 0.5, 1.0),
                        PumpTagClassifier.Kind.Power =>
                            RealtimeValueGenerator.GenerateAnalog(tag.TagId, 140, 40, 2.0),
                        PumpTagClassifier.Kind.Energy =>
                            RealtimeValueGenerator.GenerateAnalog(tag.TagId, 2500, 80, 8.0),
                        PumpTagClassifier.Kind.WaterLevel =>
                            RealtimeValueGenerator.GenerateAnalog(tag.TagId, 3.8, 0.6, 3.5),
                        PumpTagClassifier.Kind.Temperature =>
                            RealtimeValueGenerator.GenerateAnalog(tag.TagId, 55, 12, 2.5),
                        PumpTagClassifier.Kind.TempSetpoint =>
                            36.5 + (tag.TagId % 5) * 0.4,
                        PumpTagClassifier.Kind.CurrentSetpoint =>
                            36.0 + (tag.TagId % 4) * 0.5,
                        _ => RealtimeValueGenerator.Generate(tag.DataType, tag.TagId)
                    };
                    quality = TagQualityNames.Good;
                }

                await store.SetAsync(tag.TagId, new RealtimeValue
                {
                    TagId = tag.TagId,
                    Value = value,
                    Timestamp = now,
                    Quality = quality
                }, cancellationToken);

                var groups = screenSlugs
                    .SelectMany(slug => new[]
                    {
                        ScadaRealtimeGroups.StationScreen(tag.StationId, slug),
                        ScadaRealtimeGroups.StationScreenDevice(tag.StationId, slug, tag.DeviceId)
                    })
                    .Distinct()
                    .ToArray();

                await broadcaster.PublishAsync(new RealtimeChangedMessage
                {
                    TagId = tag.TagId,
                    DeviceId = tag.DeviceId,
                    StationId = tag.StationId,
                    PlcCode = tag.PlcCode,
                    DeviceCode = tag.DeviceCode,
                    TagCode = tag.Code,
                    TagName = tag.TagName,
                    Value = value,
                    DataType = tag.DataType,
                    Quality = quality,
                    Timestamp = now
                }, groups, cancellationToken);
            }
        }
    }

    private static bool IsPump(string? deviceType, string? deviceCode)
    {
        var raw = $"{deviceType} {deviceCode}".ToUpperInvariant();
        return raw.Contains("PUMP", StringComparison.Ordinal) || raw.Contains("BOM", StringComparison.Ordinal);
    }

    private static bool IsAuxRealtimeDevice(string? deviceType, string? deviceCode)
    {
        var code = deviceCode ?? string.Empty;
        var type = deviceType ?? string.Empty;
        if (code.Contains("Level", StringComparison.OrdinalIgnoreCase)) return true;
        if (System.Text.RegularExpressions.Regex.IsMatch(code, @"^Met+er\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return true;
        if (type.Contains("Meter", StringComparison.OrdinalIgnoreCase)) return true;
        if (type.Contains("Sensor", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private sealed record SimTagRow(
        long TagId,
        long DeviceId,
        long StationId,
        string PlcCode,
        string DeviceCode,
        string DeviceType,
        string Code,
        string TagName,
        string DataType,
        Domain.Enums.ScadaScreenType ScreenType);
}
