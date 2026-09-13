namespace Backend.Shared.Constants;

/// <summary>
/// Field length limits and regex patterns shared between FluentValidation
/// validators (Application layer) and EF Core column configurations
/// (Infrastructure layer), so both always agree on the same limits.
/// </summary>
public static class ValidationConstants
{
    public const int NameMaxLength = 100;
    public const int DisplayNameMaxLength = 150;
    public const int UsernameMaxLength = 100;
    public const int EmailMaxLength = 256;
    public const int PhoneNumberMaxLength = 20;
    public const int DescriptionMaxLength = 500;
    public const int KeywordMaxLength = 200;
    public const int ReasonMaxLength = 500;
    public const int PasswordHashMaxLength = 500;
    public const int TokenMaxLength = 500;
    public const int IpAddressMaxLength = 64;
    public const int AuditUserMaxLength = 100;

    public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    /// <summary>
    /// Identifier / key whitelist (username, code). Not for Vietnamese free-text.
    /// Letters, digits, dot, underscore, hyphen — no whitespace or control chars.
    /// </summary>
    public const string IdentifierPattern = @"^[A-Za-z0-9._-]+$";
}
