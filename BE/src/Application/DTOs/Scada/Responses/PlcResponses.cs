namespace Backend.Application.DTOs.Scada;

public class PlcDto
{
    public long Id { get; set; }
    public long StationId { get; set; }
    public string? StationCode { get; set; }
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
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
