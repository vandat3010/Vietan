namespace Backend.Application.Options;

/// <summary>
/// Bound from the "Password" section of appsettings.
/// Complexity + expiry — not login lockout, not session idle.
/// </summary>
public class PasswordPolicyOptions
{
    public const string SectionName = "Password";

    /// <summary>Password lifetime in days. Must be &gt; 0.</summary>
    public int ExpireDays { get; set; } = 90;

    /// <summary>Warn the user this many days before expiry. Must be &gt;= 0 and &lt; ExpireDays.</summary>
    public int WarnBeforeDays { get; set; } = 14;

    /// <summary>How often the background worker marks expired passwords. Must be &gt; 0.</summary>
    public int ExpirationCheckIntervalMinutes { get; set; } = 60;

    // --- Complexity (shared by Register / CreateUser / ChangePassword) ---

    public int MinLength { get; set; } = 8;

    /// <summary>Upper bound for user-submitted passwords (DoS guard + FE create form).</summary>
    public int MaxLength { get; set; } = 32;

    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecial { get; set; } = true;
}
