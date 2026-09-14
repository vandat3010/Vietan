namespace Backend.Application.Common;

/// <summary>
/// Password complexity rules (length + character classes). Bound from
/// <c>Password</c> section alongside expiry options — separate from login lockout
/// and session idle policy.
/// </summary>
public static class PasswordComplexity
{
    public const int DefaultMinLength = 8;
    public const int DefaultMaxLength = 32;

    public static bool TryValidate(
        string? password,
        int minLength,
        int maxLength,
        bool requireUppercase,
        bool requireLowercase,
        bool requireDigit,
        bool requireSpecial,
        out string error)
    {
        if (string.IsNullOrEmpty(password) || password.Length < minLength)
        {
            error = $"Password must be at least {minLength} characters.";
            return false;
        }

        if (password.Length > maxLength)
        {
            error = $"Password must be at most {maxLength} characters.";
            return false;
        }

        if (requireUppercase && !password.Any(char.IsUpper))
        {
            error = "Password must contain at least one uppercase letter.";
            return false;
        }

        if (requireLowercase && !password.Any(char.IsLower))
        {
            error = "Password must contain at least one lowercase letter.";
            return false;
        }

        if (requireDigit && !password.Any(char.IsDigit))
        {
            error = "Password must contain at least one digit.";
            return false;
        }

        if (requireSpecial && !password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            error = "Password must contain at least one special character.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
