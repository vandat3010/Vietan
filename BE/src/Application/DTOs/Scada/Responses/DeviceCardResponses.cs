namespace Backend.Application.DTOs.Scada;

/// <summary>
/// Card thiết bị theo chuỗi DB: station → plc → device → tag → history_1s.
/// </summary>
public class StationDeviceCardsDto
{
    public StationDetailDto Station { get; set; } = new();
    public IReadOnlyList<DeviceCardItemDto> Items { get; set; } = [];
}

/// <summary>1 thiết bị + PLC cha + toàn bộ tag + giá trị history mới nhất.</summary>
public class DeviceCardItemDto
{
    public PlcDto Plc { get; set; } = new();
    public DeviceDto Device { get; set; } = new();
    public IReadOnlyList<DeviceCardTagReadingDto> Tags { get; set; } = [];
}

/// <summary>scada.tag + latest history.history_1s (join tag_id).</summary>
public class DeviceCardTagReadingDto
{
    public long Id { get; set; }
    public long PlcId { get; set; }
    public long DeviceId { get; set; }
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
    public double? Value { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
