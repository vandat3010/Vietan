namespace Backend.Infrastructure.Identity;

/// <summary>
/// Bound from the "Jwt" section of appsettings.json. Kept in Infrastructure
/// (not Application) because JWT signing/validation is an infrastructure detail -
/// the Application layer only ever talks to <see cref="Backend.Application.Interfaces.Services.ITokenService"/>.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string SigningKey { get; set; } = default!;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Fail-fast rules for JWT config. Signing key must come from user-secrets /
    /// environment in production — placeholders and short keys are rejected.
    /// Never log <see cref="SigningKey"/>.
    /// </summary>
    public bool Validate(out string error)
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            error = "Jwt:Issuer is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            error = "Jwt:Audience is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SigningKey) || SigningKey.Length < 32)
        {
            error = "Jwt:SigningKey must be at least 32 characters (set Jwt__SigningKey from a secret store).";
            return false;
        }

        if (IsPlaceholderSigningKey(SigningKey))
        {
            error = "Jwt:SigningKey is a placeholder. Set Jwt__SigningKey from user-secrets, environment, or a secret manager.";
            return false;
        }

        if (AccessTokenExpirationMinutes is < 1 or > 60)
        {
            error = "Jwt:AccessTokenExpirationMinutes must be between 1 and 60 (recommended 10–15).";
            return false;
        }

        if (RefreshTokenExpirationDays is < 1 or > 30)
        {
            error = "Jwt:RefreshTokenExpirationDays must be between 1 and 30.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool IsPlaceholderSigningKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        var trimmed = key.Trim();
        if (trimmed.Equals("secret", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("123456", StringComparison.Ordinal)
            || trimmed.Equals("development-secret", StringComparison.OrdinalIgnoreCase))
            return true;

        return trimmed.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
    }
}
