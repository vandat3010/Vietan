namespace Backend.Infrastructure.Common;

/// <summary>Bound from the "FileStorage" section of appsettings.json.</summary>
public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    /// <summary>Absolute or content-root-relative directory used by <see cref="LocalFileStorageService"/>.</summary>
    public string RootPath { get; set; } = "wwwroot/uploads";

    /// <summary>Public base URL that maps to <see cref="RootPath"/> (e.g. "https://api.example.com/uploads").</summary>
    public string PublicBaseUrl { get; set; } = "/uploads";
}
