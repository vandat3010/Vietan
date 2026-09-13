using Backend.Application.DTOs.Industrial;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Industrial;

/// <summary>
/// Forward-looking contract for the alarm subsystem: no implementation is
/// registered yet, so resolving this from DI will fail until one is added. The
/// eventual implementation belongs in <c>src\Infrastructure\Industrial\</c>; the
/// contract lives here so alarm endpoints and notification workers can be built
/// against the acknowledge/clear lifecycle before that work starts.
/// </summary>
public interface IAlarmService
{
    Task<Result<AlarmDto>> CreateAlarmAsync(CreateAlarmDto request, CancellationToken cancellationToken = default);

    /// <summary>Records that an operator has seen the alarm; the optional note captures why, which is what audits and shift handovers actually need.</summary>
    Task<Result> AcknowledgeAlarmAsync(Guid alarmId, string? note, CancellationToken cancellationToken = default);

    /// <summary>Manual clear for conditions the device cannot report as resolved on its own.</summary>
    Task<Result> ClearAlarmAsync(Guid alarmId, string? note, CancellationToken cancellationToken = default);

    /// <summary>Returned unpaged because the active set is bounded by design - an alarm list that needs paging is an alarm flood, not a page-two problem.</summary>
    Task<Result<IReadOnlyList<AlarmDto>>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);

    Task<Result<PaginationResult<AlarmDto>>> GetAlarmHistoryAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default);
}
