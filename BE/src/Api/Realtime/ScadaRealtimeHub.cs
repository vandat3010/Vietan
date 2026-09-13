using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Application.Realtime;
using Backend.Application.Scada;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Api.Realtime;

/// <summary>
/// Realtime delta hub. Pattern: REST or Snapshot event = initial state; TagChanged = deltas.
/// Open for screen viewing (no JWT required). User/IAM APIs remain authorized separately.
/// </summary>
public sealed class ScadaRealtimeHub(IServiceScopeFactory scopeFactory) : Hub
{
    public const string TagChangedEvent = "TagChanged";
    public const string SnapshotEvent = "Snapshot";

    public async Task Subscribe(long stationId, string screen, long? deviceId = null)
    {
        if (!ScadaScreenMapping.TryParse(screen, out var screenType))
            throw new HubException("Unknown screen.");

        var slug = ScadaScreenMapping.ToSlug(screenType);
        await Groups.AddToGroupAsync(Context.ConnectionId, ScadaRealtimeGroups.StationScreen(stationId, slug));
        if (deviceId is { } id)
            await Groups.AddToGroupAsync(Context.ConnectionId, ScadaRealtimeGroups.StationScreenDevice(stationId, slug, id));

        // Initial snapshot to caller only (FE may also use REST device-monitor/schematic).
        try
        {
            using var scope = scopeFactory.CreateScope();
            var screens = scope.ServiceProvider.GetRequiredService<IScreenRealtimeQueryService>();
            var result = await screens.GetSnapshotAsync(
                screenType,
                new ScreenTagQuery { StationId = stationId, DeviceId = deviceId },
                Context.ConnectionAborted);

            if (result.IsSuccess && result.Value is not null)
            {
                await Clients.Caller.SendAsync(
                    SnapshotEvent,
                    result.Value,
                    Context.ConnectionAborted);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected during subscribe.
        }
    }

    public async Task Unsubscribe(long stationId, string screen, long? deviceId = null)
    {
        if (!ScadaScreenMapping.TryParse(screen, out var screenType))
            return;

        var slug = ScadaScreenMapping.ToSlug(screenType);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ScadaRealtimeGroups.StationScreen(stationId, slug));
        if (deviceId is { } id)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ScadaRealtimeGroups.StationScreenDevice(stationId, slug, id));
    }
}
