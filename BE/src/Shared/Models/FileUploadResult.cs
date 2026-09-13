namespace Backend.Shared.Models;

/// <summary>
/// Result of a successful upload through <c>IFileStorageService</c>. Shared
/// (not Application-only) because Api layer file-upload endpoints may want to
/// return this shape directly without an extra mapping step.
/// </summary>
public class FileUploadResult
{
    public required string FileName { get; init; }
    public required string StoragePath { get; init; }
    public required long SizeInBytes { get; init; }
    public required string ContentType { get; init; }
}
