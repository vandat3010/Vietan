namespace Backend.Domain.Entities.History;

/// <summary>
/// 1-second trend samples. Timescale hypertable <c>history.history_1s</c>.
/// No surrogate Id; no EF navigation/FK to <c>scada.tag</c> — join by TagId in the API.
/// </summary>
public class History1s
{
    public DateTimeOffset Time { get; set; }

    public long TagId { get; set; }

    public double Value { get; set; }
}
