using Backend.Application.DTOs.Users;
using Backend.Domain.Entities;

namespace Backend.Application.Mapping;

/// <summary>
/// Hand-written mapping extension methods instead of a reflection-based mapper
/// (AutoMapper/Mapster). For a template like this, explicit mappings are easier
/// to debug, have zero runtime "magic", and avoid taking on an extra
/// dependency/licensing surface - swap in a mapper library here if preferred.
/// </summary>
public static class UserMappingExtensions
{
    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Email = user.Email.Value,
        FirstName = user.FullName.FirstName,
        LastName = user.FullName.LastName,
        PhoneNumber = user.PhoneNumber,
        Status = user.Status.ToString(),
        LastLoginAtUtc = user.LastLoginAtUtc,
        CreatedDate = user.CreatedDate,
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
    };

    public static IReadOnlyList<UserDto> ToDto(this IEnumerable<User> users) =>
        users.Select(u => u.ToDto()).ToList();
}
