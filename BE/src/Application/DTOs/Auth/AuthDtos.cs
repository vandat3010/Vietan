namespace Backend.Application.DTOs.Auth;

public class RegisterDto
{
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginDto
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = default!;
}

public class RevokeTokenDto
{
    public string RefreshToken { get; set; } = default!;
}

public class ResetPasswordDto
{
    public Guid UserId { get; set; }
    public string NewPassword { get; set; } = default!;
}

public class AuthResultDto
{
    public string AccessToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = default!;
    public IReadOnlyList<string> Roles { get; set; } = [];
}
