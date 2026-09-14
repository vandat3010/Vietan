using Backend.Application.DTOs.Industrial;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Industrial;

/// <summary>
/// Legacy Industrial Guid contract — <b>not registered in DI</b>.
/// Production acknowledge/clear uses <c>IAlarmHistoryQueryService</c>
/// against Timescale <c>alarm_history</c> (long id):
/// <c>POST /api/v1/alarm-histories/{id}/acknowledge|clear</c>.
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
