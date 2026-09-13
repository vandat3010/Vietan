using Backend.Application.DTOs.Roles;
using Backend.Domain.Entities;

namespace Backend.Application.Mapping;

public static class RoleMappingExtensions
{
    public static RoleDto ToDto(this Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsSystemRole = role.IsSystemRole,
        Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList()
    };

    public static IReadOnlyList<RoleDto> ToDto(this IEnumerable<Role> roles) =>
        roles.Select(r => r.ToDto()).ToList();
}
