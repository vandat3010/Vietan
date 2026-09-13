namespace Backend.Domain.Entities.History;

/// <summary>
/// 1-minute samples. Timescale hypertable <c>history.history_1m</c>.
/// </summary>
public class History1m
{
    public DateTimeOffset Time { get; set; }

    public long TagId { get; set; }

    public double Value { get; set; }
}
