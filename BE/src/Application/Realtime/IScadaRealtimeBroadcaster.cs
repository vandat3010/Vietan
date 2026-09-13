namespace Backend.Application.Realtime;

public sealed class RealtimeChangedMessage
{
    public long TagId { get; init; }
    public long DeviceId { get; init; }
    public long StationId { get; init; }
    public string? PlcCode { get; init; }
    public string? DeviceCode { get; init; }
    public string TagCode { get; init; } = string.Empty;
    public string? TagName { get; init; }
    public object? Value { get; init; }
    public string DataType { get; init; } = string.Empty;
    public string Quality { get; init; } = TagQualityNames.Good;
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>Pushes tag changes to SignalR groups. Implemented in the Api layer.</summary>
public interface IScadaRealtimeBroadcaster
{
    Task PublishAsync(
        RealtimeChangedMessage message,
        IReadOnlyCollection<string> groups,
        CancellationToken cancellationToken = default);
}
