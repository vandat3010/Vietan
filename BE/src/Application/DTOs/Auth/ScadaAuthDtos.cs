namespace Backend.Application.DTOs.Auth;

/// <summary>DTOs for SCADA operator authentication against <c>scada.users</c>.</summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>ERD FullName. Preferred over <see cref="DisplayName"/>.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Legacy alias for FE; used when <see cref="FullName"/> is empty.</summary>
    public string DisplayName { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional hint only. BE prefers SessionId from refresh-token record / JWT claim.
    /// </summary>
    public string? SessionId { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Username { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    /// <summary>ERD FullName.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Legacy FE alias — same as FullName.</summary>
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    /// <summary>True when the user must change password before using business APIs (BE 1.3a).</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Whole days until password expires (BE 1.3c). Null when unknown (legacy user).</summary>
    public int? PasswordExpiresInDays { get; set; }

    /// <summary>True when within the warning window and not yet expired (BE 1.3d).</summary>
    public bool PasswordExpiringSoon { get; set; }

    // --- Profile (Phase 0). Security fields (PasswordHash/FailedLoginCount/LockoutUntil) are never exposed. ---
    public string? Email { get; set; }
    public string? Unit { get; set; }
    public int? Level { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Description { get; set; }
}

public class CurrentUserResponse
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>True when the user must change password before using business APIs (BE 1.3a).</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Whole days until password expires (BE 1.3c). Null when unknown (legacy user).</summary>
    public int? PasswordExpiresInDays { get; set; }

    /// <summary>True when within the warning window and not yet expired (BE 1.3d).</summary>
    public bool PasswordExpiringSoon { get; set; }

    // --- Profile (Phase 0). No credential/security-sensitive fields are exposed. ---
    public string? Email { get; set; }
    public string? Unit { get; set; }
    public int? Level { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Returned only in Development for forgot-password so Swagger/manual testing
/// can exercise reset without an email sender. Never populated in Production.
/// </summary>
public class ForgotPasswordResponse
{
    public string Message { get; set; } = string.Empty;
    public string? DevelopmentResetToken { get; set; }
}
