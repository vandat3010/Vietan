namespace Backend.Shared.Constants;

/// <summary>Mã lỗi nghiệp vụ SCADA (Result.Failure code).</summary>
public static class ScadaErrorCodes
{
    public const string StationNotFound = "Station.NotFound";
    public const string PlcNotFound = "Plc.NotFound";
    public const string DeviceNotFound = "Device.NotFound";
    public const string TagNotFound = "Tag.NotFound";
    public const string Unexpected = "Scada.Unexpected";
    public const string TimeRangeRequired = "History.TimeRangeRequired";
    public const string InvalidTimeRange = "History.InvalidTimeRange";
    public const string ChartUnknown = "Chart.Unknown";
    public const string ChartRangeTooLarge = "Chart.RangeTooLarge";
}
