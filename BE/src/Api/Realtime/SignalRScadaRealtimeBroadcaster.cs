using Backend.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Api.Realtime;

public sealed class SignalRScadaRealtimeBroadcaster(IHubContext<ScadaRealtimeHub> hub) : IScadaRealtimeBroadcaster
{
    public async Task PublishAsync(
        RealtimeChangedMessage message,
        IReadOnlyCollection<string> groups,
        CancellationToken cancellationToken = default)
    {
        foreach (var group in groups)
        {
            await hub.Clients.Group(group).SendAsync(ScadaRealtimeHub.TagChangedEvent, message, cancellationToken);
        }
    }
}
