namespace Backend.Application.Common;

/// <summary>
/// Provider-agnostic cache. Keys are plain strings and lifetimes are
/// <see cref="TimeSpan"/>s, so the in-memory default can be swapped for Redis
/// (or any distributed cache) through DI alone - no <c>IMemoryCache</c> or
/// <c>IDistributedCache</c> type ever reaches the Application layer.
/// <para>
/// Every member is async even though the in-memory implementation completes
/// synchronously: a distributed backend needs the network round-trip, and
/// retrofitting async onto a sync interface later would touch every caller.
/// </para>
/// </summary>
public interface ICacheService
{
    /// <summary>Returns the cached value, or <c>default</c> when the key is absent or expired.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a value. When both expirations are omitted the defaults from
    /// <c>CacheConstants</c> apply, so nothing is ever cached indefinitely by accident.
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the cached value or, on a miss, runs <paramref name="factory"/> and
    /// caches its result. This get-or-populate pattern is the actual reason caches
    /// exist; exposing it here keeps the "check, miss, load, store" dance out of
    /// every calling service.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);
}
