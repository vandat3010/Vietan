namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Defines how history is stored (interval, retention, compression).
/// A tag may link to zero or many profiles via <see cref="TagHistoryConfig"/>.
/// Mapped to <c>scada.history_profile</c>.
/// </summary>
public class HistoryProfile : ScadaEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int IntervalSecond { get; set; }

    public int RetentionDay { get; set; }

    public int? CompressionDay { get; set; }

    public string? Description { get; set; }

    public bool IsEnable { get; set; }

    public ICollection<TagHistoryConfig> TagHistoryConfigs { get; set; } = new List<TagHistoryConfig>();
}
