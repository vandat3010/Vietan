using Backend.Application.DTOs.Roles;
using Backend.Application.Interfaces.Services;
using Backend.Application.Interfaces.UnitOfWork;
using Backend.Application.Mapping;
using Backend.Domain.Entities;
using Backend.Shared.Results;

namespace Backend.Application.Services;

public class RoleService(IUnitOfWork unitOfWork) : IRoleService
{
    public async Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await unitOfWork.Roles.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<RoleDto>>.Success(roles.ToDto());
    }

    public async Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await unitOfWork.Roles.GetByIdAsync(id, cancellationToken);
        return role is null
            ? Result<RoleDto>.Failure("Role.NotFound", $"Role '{id}' was not found.")
            : Result<RoleDto>.Success(role.ToDto());
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleDto request, CancellationToken cancellationToken = default)
    {
        var existing = await unitOfWork.Roles.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
            return Result<RoleDto>.Failure("Role.AlreadyExists", $"Role '{request.Name}' already exists.");

        var role = Role.Create(request.Name, request.Description);
        foreach (var permissionId in request.PermissionIds)
            role.GrantPermission(permissionId);

        await unitOfWork.Roles.CreateAsync(role, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RoleDto>.Success(role.ToDto());
    }
}
