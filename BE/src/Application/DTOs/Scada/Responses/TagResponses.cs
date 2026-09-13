namespace Backend.Application.DTOs.Scada;

public class TagDto
{
    public long Id { get; set; }
    public long PlcId { get; set; }
    public long DeviceId { get; set; }
    public string? PlcCode { get; set; }
    public string? DeviceCode { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public double? Scale { get; set; }
    public double? OffsetValue { get; set; }
    public bool ReadOnly { get; set; }
    public bool WriteEnable { get; set; }
    public bool EnableRealtime { get; set; }
    public bool EnableAlarm { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
