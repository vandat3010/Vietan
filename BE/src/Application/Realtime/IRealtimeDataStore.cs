namespace Backend.Application.Realtime;

/// <summary>Last-known tag value. Redis key is derived in the store, never by FE.</summary>
public sealed class RealtimeValue
{
    public long TagId { get; init; }

    public object? Value { get; init; }

    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Good | Bad | Uncertain</summary>
    public string Quality { get; init; } = TagQualityNames.Good;
}

public static class TagQualityNames
{
    public const string Good = "Good";
    public const string Bad = "Bad";
    public const string Uncertain = "Uncertain";
}

/// <summary>
/// Runtime last-value store (Fake in-memory or Redis). Not a history database.
/// </summary>
public interface IRealtimeDataStore
{
    Task<RealtimeValue?> GetAsync(long tagId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<long, RealtimeValue>> GetManyAsync(
        IReadOnlyCollection<long> tagIds,
        CancellationToken cancellationToken = default);

    Task SetAsync(long tagId, RealtimeValue value, CancellationToken cancellationToken = default);
}
