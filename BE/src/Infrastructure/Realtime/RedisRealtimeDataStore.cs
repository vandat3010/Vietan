using Backend.Application.Options;
using Backend.Application.Realtime;
using Backend.Shared.Helpers;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Backend.Infrastructure.Realtime;

/// <summary>Redis last-value store. Swap with Fake via <c>Realtime:Provider</c>.</summary>
public sealed class RedisRealtimeDataStore(
    IConnectionMultiplexer redis,
    IOptions<RealtimeOptions> options) : IRealtimeDataStore
{
    public async Task<RealtimeValue?> GetAsync(long tagId, CancellationToken cancellationToken = default)
    {
        var raw = await redis.GetDatabase().StringGetAsync(Key(tagId));
        if (raw.IsNullOrEmpty)
            return null;
        return JsonHelper.Deserialize<RealtimeValue>((string)raw!);
    }

    public async Task<IReadOnlyDictionary<long, RealtimeValue>> GetManyAsync(
        IReadOnlyCollection<long> tagIds,
        CancellationToken cancellationToken = default)
    {
        var ids = tagIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<long, RealtimeValue>();

        var keys = ids.Select(id => (RedisKey)Key(id)).ToArray();
        var values = await redis.GetDatabase().StringGetAsync(keys);
        var result = new Dictionary<long, RealtimeValue>();
        for (var i = 0; i < ids.Length; i++)
        {
            if (values[i].IsNullOrEmpty)
                continue;
            var parsed = JsonHelper.Deserialize<RealtimeValue>((string)values[i]!);
            if (parsed is not null)
                result[ids[i]] = parsed;
        }

        return result;
    }

    public async Task SetAsync(long tagId, RealtimeValue value, CancellationToken cancellationToken = default)
    {
        await redis.GetDatabase().StringSetAsync(Key(tagId), JsonHelper.Serialize(value));
    }

    private string Key(long tagId) => $"{options.Value.RedisKeyPrefix}:{tagId}";
}
