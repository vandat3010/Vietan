namespace Backend.Application.DTOs.Scada;

/// <summary>
/// Response màn Devices — chỉ field hiển thị trên card.
/// Station: GET /stations/{id}. Không dump plc/tag metadata.
/// </summary>
public class StationDeviceMonitorDto
{
    public IReadOnlyList<DeviceMonitorItemDto> Items { get; set; } = [];
}

/// <summary>1 card bơm — đúng UI.</summary>
public class DeviceMonitorItemDto
{
    public long DeviceId { get; set; }

    /// <summary>Header: "Bơm 1".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Công suất định mức (kW) — "160kW".</summary>
    public double? RatedPowerKw { get; set; }

    /// <summary>running | stopped | error | maintenance | unknown</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Nhiệt độ cuộn dây thực tế — Cuộn A/B/C.</summary>
    public DeviceMonitorAbcValueDto WindingTempActual { get; set; } = new();

    /// <summary>Nhiệt độ cuộn dây cho phép.</summary>
    public DeviceMonitorAbcValueDto WindingTempAllowed { get; set; } = new();

    /// <summary>Ổ bi trên — thực tế / cho phép.</summary>
    public DeviceMonitorActualAllowedDto BearingTop { get; set; } = new();

    /// <summary>Ổ bi dưới — thực tế / cho phép.</summary>
    public DeviceMonitorActualAllowedDto BearingBottom { get; set; } = new();

    /// <summary>Mực nước — Sông / Bể xả.</summary>
    public DeviceMonitorWaterLevelDto WaterLevel { get; set; } = new();

    /// <summary>Thời gian chạy — tức thời / tổng (giờ).</summary>
    public DeviceMonitorRuntimeDto Runtime { get; set; } = new();

    /// <summary>Thông số điện.</summary>
    public DeviceMonitorElectricalDto Electrical { get; set; } = new();
}

public class DeviceMonitorAbcValueDto
{
    public double? A { get; set; }
    public double? B { get; set; }
    public double? C { get; set; }
}

public class DeviceMonitorActualAllowedDto
{
    public double? Actual { get; set; }
    public double? Allowed { get; set; }
}

public class DeviceMonitorWaterLevelDto
{
    public double? River { get; set; }
    public double? Basin { get; set; }
}

public class DeviceMonitorRuntimeDto
{
    public double? InstantH { get; set; }
    public double? TotalH { get; set; }
}

/// <summary>8 thông số điện — đúng block UI.</summary>
public class DeviceMonitorElectricalDto
{
    public double? VoltageRs { get; set; }
    public double? VoltageSt { get; set; }
    public double? VoltageTr { get; set; }
    public double? CurrentA { get; set; }
    public double? PowerFactor { get; set; }
    public double? FrequencyHz { get; set; }
    public double? PowerKw { get; set; }
    public double? EnergyKwh { get; set; }
}
