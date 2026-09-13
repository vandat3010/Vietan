using Backend.Application.Common;
using Backend.Shared.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Infrastructure.Common;

/// <summary>
/// Default <see cref="ICacheService"/> backed by the process-local
/// <see cref="IMemoryCache"/>. Fine for a single node; behind a load balancer
/// every instance keeps its own copy, so entries can be stale on node B after
/// node A invalidates them. Swap the DI registration for a Redis implementation
/// once you scale out - no Application code changes. See README "Extensibility".
/// <para>
/// Differences a Redis implementation will not share: keys here are never
/// enumerable (<see cref="IMemoryCache"/> exposes no "list keys" API, hence
/// <see cref="ExistsAsync"/> is implemented as a plain lookup rather than a
/// Redis <c>EXISTS</c>), values are stored as live object references instead of
/// serialized blobs, and everything is lost on restart.
/// </para>
/// </summary>
public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    /// <remarks>
    /// When the caller specifies neither expiration both defaults from
    /// <see cref="CacheConstants"/> are applied. When the caller specifies one,
    /// the other is left unset on purpose: silently attaching a 10-minute sliding
    /// window to an explicit 12-hour absolute lifetime would evict entries long
    /// before the caller asked for it.
    /// </remarks>
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (absoluteExpiration is null && slidingExpiration is null)
        {
            absoluteExpiration = TimeSpan.FromMinutes(CacheConstants.DefaultAbsoluteExpirationMinutes);
            slidingExpiration = TimeSpan.FromMinutes(CacheConstants.DefaultSlidingExpirationMinutes);
        }

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
            SlidingExpiration = slidingExpiration
        };

        cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return Task.FromResult(cache.TryGetValue(key, out _));
    }

    /// <remarks>
    /// Concurrent misses on the same key each run <paramref name="factory"/> - the
    /// last one wins. Locking would serialize unrelated keys for a cache whose
    /// misses are cheap by definition; a Redis implementation that needs true
    /// stampede protection can add a distributed lock at this seam.
    /// </remarks>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (cache.TryGetValue(key, out T? cached) && cached is not null) return cached;

        var created = await factory(cancellationToken);
        await SetAsync(key, created, absoluteExpiration, cancellationToken: cancellationToken);

        return created;
    }
}
