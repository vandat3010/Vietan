namespace Backend.Application.DTOs.Scada;

/// <summary>Response thông số điện theo trạm.</summary>
public class StationElectricalDto
{
    public StationElectricalStationDto Station { get; set; } = new();
    public IReadOnlyList<DeviceElectricalDto> Items { get; set; } = [];
}

public class StationElectricalStationDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>Thông số điện của 1 bơm.</summary>
public class DeviceElectricalDto
{
    public DeviceElectricalEquipmentDto Equipment { get; set; } = new();
    public IReadOnlyList<ElectricalParameterDto> Parameters { get; set; } = [];
}

public class DeviceElectricalEquipmentDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class ElectricalParameterDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public long? TagId { get; set; }
    public string? Code { get; set; }
    public double? Value { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
