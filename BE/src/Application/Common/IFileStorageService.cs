using Backend.Shared.Models;

namespace Backend.Application.Common;

/// <summary>
/// Storage-agnostic file operations. The interface lives in the Application layer
/// (the consumer) while implementations live in Infrastructure, so switching
/// Local disk -> Azure Blob -> MinIO/S3 is a DI registration change only and no
/// Application/Domain code is touched. See README "Extensibility".
/// </summary>
public interface IFileStorageService
{
    /// <summary>Stores a stream and returns where it landed plus its metadata.</summary>
    Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the stored file for reading. The caller owns and must dispose the
    /// stream; returning a stream rather than a byte[] keeps large files off the
    /// large object heap and lets controllers stream straight to the response.
    /// </summary>
    Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously uploaded file. Returns false when it no longer exists.</summary>
    Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a URL clients can use to download the file. Implementations backed
    /// by object storage may return a time-limited pre-signed URL, hence the
    /// optional <paramref name="expiresIn"/> and the async signature.
    /// </summary>
    Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default);
}
