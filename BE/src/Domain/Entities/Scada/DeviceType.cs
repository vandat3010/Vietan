namespace Backend.Domain.Entities.Scada;

/// <summary>ERD DeviceType lookup — <c>scada.device_type</c>.</summary>
public class DeviceType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
