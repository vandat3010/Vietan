using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

public class StationQuery : PaginationRequest
{
    public bool? IsActive { get; set; }
}

public class StationElectricalQuery
{
    public long? DeviceId { get; set; }
    public string? DeviceType { get; set; }
}

public class StationDeviceCardsQuery
{
    public long? DeviceId { get; set; }
    public string? DeviceType { get; set; }
    public bool? IsEnable { get; set; }
}
