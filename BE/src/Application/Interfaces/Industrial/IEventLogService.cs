using Backend.Application.DTOs.Industrial;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Industrial;

/// <summary>
/// Forward-looking contract for the industrial event journal: no implementation is
/// registered yet, so resolving this from DI will fail until one is added. It is
/// kept apart from the application's own audit logging because plant events are
/// operational records queried by time window and source; the implementation is
/// expected in <c>src\Infrastructure\Industrial\</c>.
/// </summary>
public interface IEventLogService
{
    Task<Result> WriteEventAsync(IndustrialEventDto industrialEvent, CancellationToken cancellationToken = default);

    Task<Result<PaginationResult<IndustrialEventDto>>> GetEventsAsync(EventLogQuery query, CancellationToken cancellationToken = default);
}
