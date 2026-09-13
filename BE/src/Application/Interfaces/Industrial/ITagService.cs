using Backend.Application.DTOs.Industrial;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Industrial;

/// <summary>
/// Forward-looking contract for historian tag reads and writes: no implementation
/// is registered yet, so resolving this from DI will fail until one is added. It is
/// declared now so the ingestion and trending shapes are fixed before the
/// TimescaleDB-backed implementation lands in <c>src\Infrastructure\Industrial\</c>.
/// </summary>
public interface ITagService
{
    /// <summary>Current value for live displays, separated from history so it can be served from a last-value cache instead of a hypertable scan.</summary>
    Task<Result<TagValueDto>> GetLatestValueAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TagValueDto>>> GetHistoricalDataAsync(TagHistoryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Single-sample write, for event-driven or manual entry only - polling paths should use <see cref="SaveTagValuesAsync"/>.</summary>
    Task<Result> SaveTagValueAsync(TagValueDto value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch write. SCADA polling writes arrive in bursts - hundreds or thousands of
    /// tags landing on the same scan tick - so committing them in one round trip
    /// rather than one per sample is the difference between a usable and an
    /// unusable ingestion path on TimescaleDB.
    /// </summary>
    Task<Result> SaveTagValuesAsync(IEnumerable<TagValueDto> values, CancellationToken cancellationToken = default);
}
