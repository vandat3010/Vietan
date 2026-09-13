using Backend.Domain.Common;

namespace Backend.Application.Interfaces.Repositories;

/// <summary>
/// Soft-delete operations, kept in a separate contract constrained to
/// <see cref="ISoftDelete"/> rather than bolted onto
/// <see cref="IGenericRepository{T}"/>: that way "restore this record" is a
/// compile-time error for entities that are hard-deleted, instead of a runtime
/// surprise. Inject this only for aggregates that really are logically deleted.
/// </summary>
public interface ISoftDeleteRepository<T> : IGenericRepository<T>
    where T : BaseEntity<Guid>, ISoftDelete
{
    /// <summary>Flags the entity as deleted (IsDeleted/DeletedDate/DeletedBy) instead of removing the row.</summary>
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Un-deletes a previously soft-deleted entity.</summary>
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The recycle bin: soft-deleted rows only. Bypasses the global query filter,
    /// which is the one legitimate reason to call <c>IgnoreQueryFilters</c>.
    /// </summary>
    Task<IReadOnlyList<T>> GetDeletedAsync(CancellationToken cancellationToken = default);
}
