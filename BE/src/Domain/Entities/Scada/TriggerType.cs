namespace Backend.Domain.Entities.Scada;

/// <summary>ERD TriggerType lookup — <c>scada.trigger_type</c>.</summary>
public class TriggerType
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnable { get; set; } = true;

    public ICollection<TagEventConfig> TagEventConfigs { get; set; } = new List<TagEventConfig>();
}
