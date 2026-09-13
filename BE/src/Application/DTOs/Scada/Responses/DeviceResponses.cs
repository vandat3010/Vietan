namespace Backend.Application.DTOs.Scada;

public class DeviceDto
{
    public long Id { get; set; }
    public long PlcId { get; set; }
    public string? PlcCode { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>ERD IsActive.</summary>
    public bool IsActive { get; set; }
    /// <summary>Alias for FE compatibility.</summary>
    public bool IsEnable { get => IsActive; set => IsActive = value; }
    public int? DeviceTypeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
