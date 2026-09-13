namespace Backend.Domain.Entities.History;

/// <summary>
/// 30-second trend samples. Timescale hypertable <c>history.history_30s</c>.
/// No surrogate Id; join to <c>scada.tag</c> by TagId in the API.
/// </summary>
public class History30s
{
    public DateTimeOffset Time { get; set; }

    public long TagId { get; set; }

    public double Value { get; set; }
}
