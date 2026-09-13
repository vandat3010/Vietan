namespace Backend.Domain.Entities.History;

/// <summary>
/// 30-minute long-term samples. Timescale hypertable <c>history.history_30m</c>.
/// </summary>
public class History30m
{
    public DateTimeOffset Time { get; set; }

    public long TagId { get; set; }

    public double Value { get; set; }
}
