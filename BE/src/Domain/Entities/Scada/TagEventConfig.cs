namespace Backend.Domain.Entities.Scada;

/// <summary>
/// ERD TagEventConfig — alarm/event condition on a Tag.
/// Mapped to <c>scada.tag_event_config</c>.
/// </summary>
public class TagEventConfig : ScadaEntity
{
    public long TagId { get; set; }

    public long EventTypeId { get; set; }

    public long TriggerTypeId { get; set; }

    public double? TriggerValue { get; set; }

    public double? Deadband { get; set; }

    public string? Message { get; set; }

    public string? TroubleshootingGuide { get; set; }

    public int Severity { get; set; }

    public bool IsEnable { get; set; } = true;

    public string? FunctionDescription { get; set; }

    public Tag Tag { get; set; } = null!;

    public EventType EventType { get; set; } = null!;

    public TriggerType TriggerType { get; set; } = null!;
}
