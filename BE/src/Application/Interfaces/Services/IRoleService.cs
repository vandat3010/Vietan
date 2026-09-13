using Backend.Application.DTOs.Roles;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

public interface IRoleService
{
    Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<RoleDto>> CreateAsync(CreateRoleDto request, CancellationToken cancellationToken = default);
}
