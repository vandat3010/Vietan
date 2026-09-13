namespace Backend.Domain.Entities.Scada;

/// <summary>
/// Smallest addressable PLC data point. <see cref="ScadaEntity.Id"/> (TagId)
/// is the system-wide identifier used by Redis realtime keys and Timescale history.
/// Mapped to <c>scada.tag</c>.
/// </summary>
public class Tag : ScadaEntity
{
    public long PlcId { get; set; }

    public long DeviceId { get; set; }

    public string Code { get; set; } = string.Empty;

    /// <summary>PLC tag name (e.g. TempA). Column <c>tag</c>.</summary>
    public string TagName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string Address { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public double? Scale { get; set; }

    /// <summary>Column <c>offset_value</c> (avoids C# keyword <c>offset</c>).</summary>
    public double? OffsetValue { get; set; }

    public bool ReadOnly { get; set; }

    public bool WriteEnable { get; set; }

    public bool EnableRealtime { get; set; }

    public bool EnableAlarm { get; set; }

    public string? Description { get; set; }

    /// <summary>ERD IsActive.</summary>
    public bool IsActive { get; set; } = true;

    public Plc Plc { get; set; } = null!;

    public Device Device { get; set; } = null!;

    public ICollection<TagHistoryConfig> TagHistoryConfigs { get; set; } = new List<TagHistoryConfig>();

    public ICollection<TagScreenMapping> ScreenMappings { get; set; } = new List<TagScreenMapping>();

    public ICollection<TagEventConfig> TagEventConfigs { get; set; } = new List<TagEventConfig>();
}
