using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

/// <summary>Query danh sách bơm + thông số theo stationId (màn Devices).</summary>
public class StationDeviceMonitorQuery : PaginationRequest
{
    public long? DeviceId { get; set; }
    public string? DeviceType { get; set; }
    public bool? IsEnable { get; set; }
}
