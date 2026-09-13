using Backend.Application.DTOs.Users;
using Backend.Domain.Entities;
using Backend.Shared.Pagination;

namespace Backend.Application.Interfaces.Repositories;

/// <summary>
/// User-specific queries that don't fit the generic repository (e.g. lookups by
/// email, or eager-loading roles for the auth flow).
/// </summary>
public interface IUserRepository : ISoftDeleteRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keyword + field search with paging. Lives here rather than on
    /// <see cref="IGenericRepository{T}"/> because only this repository knows
    /// which User columns are searchable (and which of them are indexed).
    /// </summary>
    Task<PaginationResult<User>> SearchAsync(UserSearchQuery query, CancellationToken cancellationToken = default);
}
