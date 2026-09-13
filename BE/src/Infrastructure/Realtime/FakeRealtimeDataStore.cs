using System.Collections.Concurrent;
using Backend.Application.Realtime;

namespace Backend.Infrastructure.Realtime;

/// <summary>In-memory last-value store for environments without a Redis server.</summary>
public sealed class FakeRealtimeDataStore : IRealtimeDataStore
{
    private readonly ConcurrentDictionary<long, RealtimeValue> _values = new();

    public Task<RealtimeValue?> GetAsync(long tagId, CancellationToken cancellationToken = default)
    {
        _values.TryGetValue(tagId, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyDictionary<long, RealtimeValue>> GetManyAsync(
        IReadOnlyCollection<long> tagIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<long, RealtimeValue>();
        foreach (var id in tagIds)
        {
            if (_values.TryGetValue(id, out var value))
                result[id] = value;
        }

        return Task.FromResult((IReadOnlyDictionary<long, RealtimeValue>)result);
    }

    public Task SetAsync(long tagId, RealtimeValue value, CancellationToken cancellationToken = default)
    {
        _values[tagId] = value;
        return Task.CompletedTask;
    }
}
