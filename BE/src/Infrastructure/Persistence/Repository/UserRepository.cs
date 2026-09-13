using Backend.Application.Common;
using Backend.Application.DTOs.Users;
using Backend.Application.Interfaces.Repositories;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Extensions;
using Backend.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Repository;

public class UserRepository(
    ApplicationDbContext context,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : SoftDeleteRepository<User>(context, currentUserService, dateTimeProvider), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        // EF.Property<string> reads the raw (post-conversion) column value directly,
        // which sidesteps any ambiguity around translating the Email value object's
        // custom equality operator into SQL.
        return await Context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => EF.Property<string>(u, "Email") == normalized, cancellationToken);
    }

    public async Task<User?> GetWithRolesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        await Context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken), cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await Context.Users.AnyAsync(u => EF.Property<string>(u, "Email") == normalized, cancellationToken);
    }

    public async Task<PaginationResult<User>> SearchAsync(UserSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var users = Context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        // The only place allowed to see soft-deleted users, and only on request.
        if (query.IncludeDeleted) users = users.IgnoreQueryFilters();

        if (query.HasKeyword)
        {
            var keyword = query.Keyword!.Trim().ToLowerInvariant();

            // EF.Property reaches the converted Email/FirstName/LastName columns,
            // so the whole predicate stays translatable to a single SQL WHERE.
            users = users.Where(u =>
                EF.Property<string>(u, "Email").Contains(keyword) ||
                EF.Property<string>(u, "FirstName").ToLower().Contains(keyword) ||
                EF.Property<string>(u, "LastName").ToLower().Contains(keyword));
        }

        if (query.Status.HasValue)
            users = users.Where(u => u.Status == query.Status.Value);

        if (query.RoleId.HasValue)
            users = users.Where(u => u.UserRoles.Any(ur => ur.RoleId == query.RoleId.Value));

        if (query.CreatedFromUtc.HasValue)
            users = users.Where(u => u.CreatedDate >= query.CreatedFromUtc.Value);

        if (query.CreatedToUtc.HasValue)
            users = users.Where(u => u.CreatedDate <= query.CreatedToUtc.Value);

        var totalCount = await users.CountAsync(cancellationToken);

        var sorts = query.GetSortDescriptors();
        users = sorts.Count > 0
            ? users.ApplySorting(sorts)
            : users.OrderByDescending(u => u.CreatedDate);

        var items = await users.ApplyPaging(query).ToListAsync(cancellationToken);

        return PaginationResult<User>.Create(items, totalCount, query);
    }
}
