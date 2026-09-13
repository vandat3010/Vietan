using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

public class DeviceQuery : PaginationRequest
{
    public long? PlcId { get; set; }
    public string? DeviceType { get; set; }
    public bool? IsEnable { get; set; }
}
