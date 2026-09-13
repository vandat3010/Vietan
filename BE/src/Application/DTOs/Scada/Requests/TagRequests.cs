using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

public class TagQuery : PaginationRequest
{
    public long? PlcId { get; set; }
    public long? DeviceId { get; set; }
    public bool? EnableRealtime { get; set; }
    public bool? EnableAlarm { get; set; }
}

public class TagHistoryConfigQuery : PaginationRequest
{
    public long? TagId { get; set; }
    public long? HistoryProfileId { get; set; }
    public bool? IsEnable { get; set; }
}
