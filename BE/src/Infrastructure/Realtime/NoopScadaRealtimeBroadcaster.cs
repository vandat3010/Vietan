using Backend.Application.Realtime;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Realtime;

public sealed class NoopScadaRealtimeBroadcaster(ILogger<NoopScadaRealtimeBroadcaster> logger) : IScadaRealtimeBroadcaster
{
    public Task PublishAsync(
        RealtimeChangedMessage message,
        IReadOnlyCollection<string> groups,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Realtime change not broadcast (noop). TagId={TagId} Groups={GroupCount}",
            message.TagId, groups.Count);
        return Task.CompletedTask;
    }
}
