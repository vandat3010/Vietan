namespace Backend.Application.DTOs.Roles;

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public IReadOnlyList<string> Permissions { get; set; } = [];
}

public class CreateRoleDto
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public List<Guid> PermissionIds { get; set; } = [];
}
