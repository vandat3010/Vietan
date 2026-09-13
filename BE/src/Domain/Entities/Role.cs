using Backend.Domain.Common;

namespace Backend.Domain.Entities;

/// <summary>
/// A named collection of permissions (e.g. "Admin", "Manager") that can be
/// assigned to users. Business rule: the built-in <c>SuperAdmin</c> role
/// cannot be renamed or deleted, enforced here rather than in a service so
/// it can never be bypassed.
/// </summary>
public class Role : AuditableEntity
{
    public const string SuperAdminRoleName = "SuperAdmin";

    private Role() { } // EF Core

    private Role(string name, string? description, bool isSystemRole)
    {
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static Role Create(string name, string? description = null, bool isSystemRole = false) =>
        new(name, description, isSystemRole);

    public void Rename(string newName, string? newDescription)
    {
        if (Name == SuperAdminRoleName)
            throw new DomainException("The SuperAdmin role cannot be renamed.");

        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Role name cannot be empty.");

        Name = newName;
        Description = newDescription;
    }

    public void GrantPermission(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId))
            return;

        _rolePermissions.Add(new RolePermission(Id, permissionId));
    }

    public void RevokePermission(Guid permissionId)
    {
        if (Name == SuperAdminRoleName)
            throw new DomainException("Cannot revoke permissions from the SuperAdmin role.");

        _rolePermissions.RemoveAll(rp => rp.PermissionId == permissionId);
    }

    public void EnsureDeletable()
    {
        if (IsSystemRole)
            throw new DomainException($"The system role '{Name}' cannot be deleted.");
    }
}
