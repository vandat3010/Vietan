using Backend.Domain.Enums;
using Backend.Shared.Pagination;

namespace Backend.Application.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = default!;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedDate { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}

/// <summary>
/// Search/filter criteria for the user list screen, on top of the standard
/// paging and sorting inherited from <see cref="PaginationRequest"/>.
/// </summary>
/// <remarks>The inherited <c>Keyword</c> is matched against email, first name and last name.</remarks>
public class UserSearchQuery : PaginationRequest
{
    public UserStatus? Status { get; set; }
    public Guid? RoleId { get; set; }
    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }
    public bool IncludeDeleted { get; set; }
}

public class CreateUserDto
{
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public List<Guid> RoleIds { get; set; } = [];
}

public class UpdateUserDto
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}

public class AssignRolesDto
{
    public List<Guid> RoleIds { get; set; } = [];
}

/// <summary>T5 — query-string wrapper so <c>reason</c> is FluentValidated (length + control chars).</summary>
public class DeactivateUserQuery
{
    public string? Reason { get; set; }
}
