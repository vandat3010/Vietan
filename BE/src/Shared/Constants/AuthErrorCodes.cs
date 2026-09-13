namespace Backend.Shared.Constants;

/// <summary>Machine-readable auth/account error codes returned to clients.</summary>
public static class AuthErrorCodes
{
    /// <summary>User is authenticated but must change password before using business APIs (BE 1.3a).</summary>
    public const string PasswordChangeRequired = "PASSWORD_CHANGE_REQUIRED";
}
