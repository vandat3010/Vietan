using Backend.Domain.Common;

namespace Backend.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many User &lt;-&gt; Role relationship.
/// </summary>
public class UserRole : BaseEntity
{
    private UserRole() { } // EF Core

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public DateTime AssignedAtUtc { get; private set; }
}
