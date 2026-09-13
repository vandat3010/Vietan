using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

/// <summary>
/// Query bảng báo cáo theo thiết bị — nguồn <c>public.history_30m</c>.
/// </summary>
public class StationReportTableQuery : PaginationRequest
{
    public DateOnly? ReportDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    /// <summary>Device.Id thuộc station (bắt buộc).</summary>
    public long DeviceId { get; set; }
}
