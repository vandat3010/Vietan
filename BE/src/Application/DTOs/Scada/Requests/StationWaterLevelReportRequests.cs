using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

/// <summary>
/// Query báo cáo mức nước — nguồn <c>history.history_30s</c>.
/// Lọc trong 1 ngày (reportDate + start/end). deviceId tùy chọn.
/// </summary>
public class StationWaterLevelReportQuery : PaginationRequest
{
    /// <summary>Ngày báo cáo. Mặc định: hôm nay (giờ VN).</summary>
    public DateOnly? ReportDate { get; set; }

    /// <summary>Giờ bắt đầu trong ngày. Mặc định 00:00:00.</summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>Giờ kết thúc trong ngày. Mặc định 23:59:59.</summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>Lọc theo thiết bị. Null = tất cả thiết bị trong trạm.</summary>
    public long? DeviceId { get; set; }

    /// <summary>Lọc theo loại thiết bị (vd Level). Null = không lọc type.</summary>
    public string? DeviceType { get; set; }
}
