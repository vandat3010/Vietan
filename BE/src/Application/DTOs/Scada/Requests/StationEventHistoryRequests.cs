using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

/// <summary>
/// Query lịch sử sự kiện theo station — nguồn <c>public.alarm_history</c>.
/// </summary>
public class StationEventHistoryQuery : PaginationRequest
{
    /// <summary>
    /// Tab: status | value-change | system.
    /// (login dùng audit riêng trên FE)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Null / 0 = tất cả thiết bị thuộc station.</summary>
    public long? DeviceId { get; set; }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
