using Backend.Application.DTOs.Scada;
using Backend.Domain.Enums;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services.Scada;

public interface IScreenRealtimeQueryService
{
    Task<Result<RealtimeSnapshotDto>> GetSnapshotAsync(
        ScadaScreenType screen,
        ScreenTagQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<PaginationResult<ScreenHistoryPointDto>>> GetHistoryAsync(
        ScadaScreenType screen,
        ScreenHistoryQuery query,
        CancellationToken cancellationToken = default);
}
