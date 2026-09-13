using System.Linq.Expressions;
using Backend.Domain.Common;
using Backend.Domain.Specifications;
using Backend.Shared.Pagination;

namespace Backend.Application.Interfaces.Repositories;

/// <summary>
/// EF Core-backed repository contract used ONLY for command-side work
/// (Create/Update/Delete + the reads needed to support commands and simple
/// list screens). Read-heavy reporting/dashboard queries go through
/// <see cref="Backend.Application.Interfaces.Dapper.IDapperRepository"/> instead -
/// see the README for the full rationale on the EF/Dapper split.
/// <para>
/// Nothing here talks SQL and nothing here contains business rules: the
/// repository's only job is persistence. Workflow lives in Application services,
/// invariants live in the Domain entities.
/// </para>
/// </summary>
public interface IGenericRepository<T> where T : BaseEntity<Guid>
{
    // ---------- Reads ----------

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// All entities matching an ad-hoc predicate. This single method replaces the
    /// usual FindAsync/FilterAsync/GetByConditionAsync trio - they would all
    /// compile to the same <c>Where(predicate)</c> call, and three names for one
    /// behaviour is how repositories rot.
    /// </summary>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Reusable, composable query encapsulated as a Specification (includes/ordering/paging).</summary>
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// One page of results with sorting applied server-side. <paramref name="filter"/>
    /// is where a caller injects its own keyword/field filtering, because a generic
    /// repository cannot know which columns of <typeparamref name="T"/> are searchable.
    /// </summary>
    Task<PaginationResult<T>> GetPagedAsync(
        PaginationRequest request,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Existence check by primary key - cheaper than loading the entity just to null-check it.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // ---------- Writes ----------
    // These only stage changes against the change tracker. Nothing reaches the
    // database until IUnitOfWork.SaveChangesAsync is called, which is exactly
    // what makes multi-repository transactions possible.

    Task CreateAsync(T entity, CancellationToken cancellationToken = default);

    Task CreateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <remarks>
    /// Synchronous on purpose: marking an already-tracked entity as modified is a
    /// pure in-memory operation. An <c>UpdateAsync</c> returning a completed Task
    /// would suggest I/O that never happens and force callers into pointless awaits.
    /// </remarks>
    void Update(T entity);

    /// <inheritdoc cref="Update"/>
    void Delete(T entity);

    void DeleteRange(IEnumerable<T> entities);

    /// <summary>
    /// Loads by id and stages the delete. Genuinely async (it hits the database to
    /// find the row) and returns false when the entity does not exist, so callers
    /// can map that to a 404 without a separate existence query.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
