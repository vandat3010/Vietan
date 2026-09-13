using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Scada;

public class PlcQuery : PaginationRequest
{
    public long? StationId { get; set; }
    public bool? IsEnable { get; set; }
}
