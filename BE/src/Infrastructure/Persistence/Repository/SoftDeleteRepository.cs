using Backend.Application.Common;
using Backend.Application.Interfaces.Repositories;
using Backend.Domain.Common;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repository;

/// <summary>
/// Adds recycle-bin behaviour on top of <see cref="GenericRepository{T}"/>.
/// <para>
/// Note that a plain <c>Delete</c> on a soft-deletable entity is ALSO turned into
/// a soft delete by ApplicationDbContext.SaveChangesAsync; the explicit methods
/// here exist for the cases where the intent must be unambiguous in the calling
/// code (admin "move to trash"/"restore" screens) and for reading deleted rows
/// back, which is impossible through the global query filter.
/// </para>
/// </summary>
public class SoftDeleteRepository<T>(
    ApplicationDbContext context,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : GenericRepository<T>(context), ISoftDeleteRepository<T>
    where T : BaseEntity<Guid>, ISoftDelete
{
    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;

        entity.IsDeleted = true;
        entity.DeletedDate = dateTimeProvider.UtcNow;
        entity.DeletedBy = CurrentUser();
        DbSet.Update(entity);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters is required here: the row is invisible to normal
        // queries precisely because it is the one we want to bring back.
        var entity = await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity is null || !entity.IsDeleted) return false;

        entity.IsDeleted = false;
        entity.DeletedDate = null;
        entity.DeletedBy = null;
        DbSet.Update(entity);

        return true;
    }

    public async Task<IReadOnlyList<T>> GetDeletedAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.IsDeleted)
            .ToListAsync(cancellationToken);

    private string CurrentUser() =>
        currentUserService.UserId?.ToString() ?? ApplicationConstants.SystemUserName;
}
