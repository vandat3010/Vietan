namespace Backend.Infrastructure.Identity;

/// <summary>Bound from the "PasswordReset" / "Auth" sections of appsettings.</summary>
public class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Role assigned on public registration. Not supplied by the client —
    /// keeps privilege escalation off the wire. Override per environment.
    /// </summary>
    public string DefaultRegisterRole { get; set; } = "Operator";

    public int PasswordResetTokenExpirationMinutes { get; set; } = 30;
}
