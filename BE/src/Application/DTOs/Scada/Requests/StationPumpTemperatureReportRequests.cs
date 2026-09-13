using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

/// <summary>Query báo cáo nhiệt độ bơm — <c>history.history_30s</c>.</summary>
public class StationPumpTemperatureReportQuery : PaginationRequest
{
    public DateOnly? ReportDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    /// <summary>Bắt buộc khi chọn "Nhiệt độ bơm N". Null = tất cả bơm (Pump).</summary>
    public long? DeviceId { get; set; }
}
