namespace Backend.Domain.Entities.Scada;

/// <summary>
/// MQTT broker connection settings. Mapped to <c>scada.mqtt_config</c>.
/// </summary>
public class MqttConfig : ScadaEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Broker { get; set; } = string.Empty;

    public int Port { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? ClientId { get; set; }

    public string? TopicPublish { get; set; }

    public string? TopicSubscribe { get; set; }

    public int KeepAlive { get; set; }

    public short Qos { get; set; }

    public bool Retain { get; set; }

    public bool UseTls { get; set; }

    public bool IsEnable { get; set; }

    public string? Description { get; set; }
}
