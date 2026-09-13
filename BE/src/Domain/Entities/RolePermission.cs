using Backend.Domain.Common;

namespace Backend.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many Role &lt;-&gt; Permission relationship.
/// Modeled explicitly (instead of a pure EF "skip navigation") so it can carry
/// its own metadata later (e.g. GrantedAtUtc, GrantedBy) without a breaking change.
/// </summary>
public class RolePermission : BaseEntity
{
    private RolePermission() { } // EF Core

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; } = default!;
}
