namespace Backend.Domain.Entities.Scada;

/// <summary>
/// PLC connection configuration belonging to a single <see cref="Station"/>.
/// Mapped to <c>scada.plc</c>.
/// </summary>
public class Plc : ScadaEntity
{
    public long StationId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string PlcType { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public int Rack { get; set; }

    public int Slot { get; set; }

    public int Port { get; set; }

    public int PollingInterval { get; set; }

    public int ReconnectInterval { get; set; }

    public int Timeout { get; set; }

    public int MaxConnection { get; set; }

    public bool IsEnable { get; set; }

    public string? Description { get; set; }

    public Station Station { get; set; } = null!;

    public ICollection<Device> Devices { get; set; } = new List<Device>();

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
