using Backend.Application.DTOs.History;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services.Scada;

/// <summary>Read-only queries over Timescale history sample hypertables.</summary>
public interface IHistorySampleQueryService
{
    Task<Result<PaginationResult<HistorySampleDto>>> Get1sAsync(HistorySampleQuery query, CancellationToken cancellationToken = default);
    Task<Result<PaginationResult<HistorySampleDto>>> Get1mAsync(HistorySampleQuery query, CancellationToken cancellationToken = default);
    Task<Result<PaginationResult<HistorySampleDto>>> Get30mAsync(HistorySampleQuery query, CancellationToken cancellationToken = default);
}

public interface IAlarmHistoryQueryService
{
    Task<Result<PaginationResult<AlarmHistoryDto>>> GetPagedAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default);
    Task<Result<AlarmHistoryDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Operator ack — sets <c>IsAcknowledged = true</c> (idempotent).</summary>
    Task<Result<AlarmHistoryDto>> AcknowledgeAsync(long id, string? note, CancellationToken cancellationToken = default);

    /// <summary>Clear open alarm — sets <c>EndTime = UtcNow</c> (keeps history row).</summary>
    Task<Result<AlarmHistoryDto>> ClearAsync(long id, string? note, CancellationToken cancellationToken = default);
}

public interface IScadaEventLogQueryService
{
    Task<Result<PaginationResult<EventLogDto>>> GetPagedAsync(EventLogQuery query, CancellationToken cancellationToken = default);
    Task<Result<EventLogDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}

public interface IUserActivityLogQueryService
{
    Task<Result<PaginationResult<UserActivityLogDto>>> GetPagedAsync(UserActivityLogQuery query, CancellationToken cancellationToken = default);
    Task<Result<UserActivityLogDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
