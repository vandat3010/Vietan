namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Communication protocol catalogue (S7, MQTT, ModbusTCP, …).
/// Mapped to <c>scada.communication_config</c>.
/// </summary>
public class CommunicationConfig : ScadaEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Protocol { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsEnable { get; set; }
}
