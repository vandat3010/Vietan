namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Many-to-many link between <see cref="Tag"/> and <see cref="HistoryProfile"/>.
/// Mapped to <c>scada.tag_history_config</c>.
/// </summary>
public class TagHistoryConfig : ScadaEntity
{
    public long TagId { get; set; }

    public long HistoryProfileId { get; set; }

    public double? Deadband { get; set; }

    public int Priority { get; set; }

    public string? Description { get; set; }

    public bool IsEnable { get; set; }

    public Tag Tag { get; set; } = null!;

    public HistoryProfile HistoryProfile { get; set; } = null!;
}
