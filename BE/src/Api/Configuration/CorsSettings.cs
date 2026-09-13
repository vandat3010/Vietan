namespace Backend.Api.Configuration;

/// <summary>Bound from the "Cors" section of appsettings.json.</summary>
public class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "DefaultCorsPolicy";

    public string[] AllowedOrigins { get; set; } = [];
}
