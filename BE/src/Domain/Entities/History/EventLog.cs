namespace Backend.Domain.Entities.History;

/// <summary>
/// System event log (API, PLC, MQTT, Redis, services).
/// Timescale hypertable on <c>time</c>. Mapped to <c>history.event_log</c>.
/// </summary>
public class EventLog
{
    public long Id { get; set; }

    public DateTimeOffset Time { get; set; }

    public string Level { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }

    public string? Machine { get; set; }
}
