using Backend.Domain.Common;

namespace Backend.Domain.Entities;

/// <summary>
/// A single fine-grained capability (e.g. "Users.Create"). Permissions are
/// grouped into Roles via <see cref="RolePermission"/> rather than assigned
/// directly to users, which keeps authorization data normalized and auditable.
/// </summary>
public class Permission : AuditableEntity
{
    private Permission() { } // EF Core

    private Permission(string name, string module, string? description)
    {
        Name = name;
        Module = module;
        Description = description;
    }

    public string Name { get; private set; } = default!;
    public string Module { get; private set; } = default!;
    public string? Description { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public static Permission Create(string name, string module, string? description = null) =>
        new(name, module, description);
}
