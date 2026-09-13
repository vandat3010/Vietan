using Backend.Application.Common;
using Backend.Shared.Exceptions;
using Backend.Shared.Helpers;
using Backend.Shared.Models;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Common;

/// <summary>
/// Default <see cref="IFileStorageService"/> implementation writing to the local
/// file system. Suitable for development and single-node deployments; swap the DI
/// registration for an Azure Blob / MinIO implementation in clustered environments
/// without touching a single line of Application code.
/// </summary>
public class LocalFileStorageService(IOptions<FileStorageSettings> options) : IFileStorageService
{
    private readonly FileStorageSettings _settings = options.Value;

    public async Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // Never trust the client-supplied name: generate our own to avoid
        // path traversal and accidental overwrites.
        var safeFileName = FileHelper.GenerateFileName(fileName);
        var relativePath = string.IsNullOrWhiteSpace(folder)
            ? safeFileName
            : Path.Combine(folder, safeFileName);

        var absolutePath = ResolvePath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using (var fileStream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        return new FileUploadResult
        {
            FileName = safeFileName,
            StoragePath = relativePath.Replace('\\', '/'),
            SizeInBytes = new FileInfo(absolutePath).Length,
            ContentType = contentType
        };
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = ResolvePath(storagePath);

        if (!File.Exists(absolutePath))
            throw new NotFoundException("File", storagePath);

        Stream stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = ResolvePath(storagePath);

        if (!File.Exists(absolutePath)) return Task.FromResult(false);

        File.Delete(absolutePath);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ResolvePath(storagePath)));

    /// <remarks>
    /// Local storage has no notion of pre-signed URLs, so <paramref name="expiresIn"/>
    /// is ignored here - it exists on the interface for object-storage backends.
    /// </remarks>
    public Task<string> GetFileUrlAsync(string storagePath, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default) =>
        Task.FromResult($"{_settings.PublicBaseUrl.TrimEnd('/')}/{storagePath.TrimStart('/')}");

    /// <summary>
    /// Resolves a stored path against the configured root and refuses anything
    /// that escapes it - storage paths come back from the database and must never
    /// be able to reach "../../appsettings.json".
    /// </summary>
    private string ResolvePath(string storagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var root = Path.GetFullPath(_settings.RootPath);
        var absolutePath = Path.GetFullPath(Path.Combine(root, storagePath));

        if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException($"Invalid storage path '{storagePath}'.", "InvalidStoragePath");

        return absolutePath;
    }
}
