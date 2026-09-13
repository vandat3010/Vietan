namespace Backend.Application.DTOs.Scada;

/// <summary>Option dropdown/combobox Thiết bị — chỉ Id + Name (Device thật).</summary>
public class StationReportDeviceOptionDto
{
    /// <summary>Device.Id thuộc station.</summary>
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>1 dòng báo cáo nhiệt độ bơm.</summary>
public class PumpTemperatureReportRowDto
{
    public DateTimeOffset Time { get; set; }
    public long DeviceId { get; set; }
    public string Pump { get; set; } = string.Empty;
    public double? TempA { get; set; }
    public double? TempB { get; set; }
    public double? TempC { get; set; }
    public double? BearingBottom { get; set; }
    public double? BearingTop { get; set; }
}
