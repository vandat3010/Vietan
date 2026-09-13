namespace Backend.Shared.Constants;

/// <summary>Message ApiResponse cho các endpoint SCADA / Stations.</summary>
public static class ScadaApiMessages
{
    // Success
    public const string StationsListOk = "Danh sách trạm bơm.";
    public const string StationDetailOk = "Chi tiết trạm.";
    public const string StationElectricalOk = "Thông số điện theo trạm.";
    public const string StationSchematicOk = "Sơ đồ nguyên lý theo trạm.";
    public const string StationDeviceCardsOk = "Thiết bị theo trạm (plc + device + tag + history).";
    public const string StationDeviceMonitorOk = "Danh sách bơm màn Devices (đủ thông số UI).";
    public const string StationWaterLevelReportOk = "Báo cáo mức nước theo trạm.";
    public const string StationReportDevicesOk = "Danh sách thiết bị báo cáo.";
    public const string StationReportTableOk = "Bảng báo cáo theo thiết bị (history_30m).";
    public const string StationEventDevicesOk = "Danh sách thiết bị lịch sử sự kiện.";
    public const string StationEventHistoryOk = "Lịch sử sự kiện theo trạm (alarm_history).";
    public const string StationPumpTemperatureReportOk = "Báo cáo nhiệt độ bơm theo trạm.";
    public const string StationChartDevicesOk = "Danh sách thiết bị đồ thị.";
    public const string StationChartHistoryOk = "Lịch sử đồ thị theo thiết bị.";

    public const string PlcsListOk = "Danh sách PLC.";
    public const string PlcDetailOk = "Chi tiết PLC.";
    public const string DevicesListOk = "Danh sách thiết bị.";
    public const string DeviceDetailOk = "Chi tiết thiết bị.";
    public const string TagsListOk = "Danh sách tag.";
    public const string TagDetailOk = "Chi tiết tag.";
    public const string ScreenSnapshotOk = "Snapshot màn hình.";
    public const string ScreenHistoryOk = "Lịch sử theo mapping màn hình.";

    // Fail (fallback khi Result không có message)
    public const string StationNotFound = "Station not found.";
    public const string PlcNotFound = "PLC not found.";
    public const string DeviceNotFound = "Device not found.";
    public const string TagNotFound = "Tag not found.";
    public const string UnexpectedError = "Đã xảy ra lỗi khi xử lý yêu cầu.";

    public static string StationNotFoundFor(long id) =>
        $"Station '{id}' was not found.";
}
