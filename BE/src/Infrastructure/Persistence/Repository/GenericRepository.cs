using System.Linq.Expressions;
using Backend.Application.Interfaces.Repositories;
using Backend.Domain.Common;
using Backend.Domain.Specifications;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Extensions;
using Backend.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repository;

/// <summary>
/// EF Core implementation of <see cref="IGenericRepository{T}"/>. Every write only
/// stages a change against the DbContext's change tracker - nothing hits the
/// database until
/// <see cref="Application.Interfaces.UnitOfWork.IUnitOfWork.SaveChangesAsync"/> is
/// called, which is what makes multi-repository transactions possible.
/// Không bắt exception tại đây — Application Service bắt và map sang <c>Result</c>/<c>ApiResponse</c>.
/// </summary>
public class GenericRepository<T>(ApplicationDbContext context) : IGenericRepository<T> where T : BaseEntity<Guid>
{
    protected readonly ApplicationDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default) =>
        await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default) =>
        await ApplySpecification(spec).AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<PaginationResult<T>> GetPagedAsync(
        PaginationRequest request,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (filter is not null) query = query.Where(filter);

        // Counted before paging, and on the same filtered query, so TotalCount
        // always matches what the client would get by walking every page.
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .ApplySorting(request.GetSortDescriptors())
            .ApplyPaging(request)
            .ToListAsync(cancellationToken);

        return PaginationResult<T>.Create(items, totalCount, request);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        await DbSet.CountAsync(cancellationToken);

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await DbSet.CountAsync(predicate, cancellationToken);

    public async Task<int> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default) =>
        await ApplySpecification(spec).CountAsync(cancellationToken);

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(e => e.Id == id, cancellationToken);

    public async Task CreateAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public async Task CreateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default) =>
        await DbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(T entity) => DbSet.Update(entity);

    public void Delete(T entity) => DbSet.Remove(entity);

    public void DeleteRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);

    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;

        DbSet.Remove(entity);
        return true;
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec) =>
        SpecificationEvaluator<T>.GetQuery(DbSet.AsQueryable(), spec);
}
