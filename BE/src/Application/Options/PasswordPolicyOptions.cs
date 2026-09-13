namespace Backend.Application.Options;

/// <summary>
/// Bound from the "Password" section of appsettings. Drives password expiration
/// (BE 1.3c–e). No business rule is hard-coded in services — they read here.
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
}
