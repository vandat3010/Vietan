namespace Backend.Shared.Helpers;

/// <summary>
/// Small, genuinely reusable file-name/size helpers shared by any upload
/// endpoint and by <c>IFileStorageService</c> implementations
/// (Local/Azure Blob/MinIO - see Infrastructure/Common/FileStorage).
/// </summary>
public static class FileHelper
{
    /// <summary>Returns the extension without the leading dot, lower-cased ("pdf", not ".PDF").</summary>
    public static string GetExtension(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var extension = Path.GetExtension(fileName);
        return extension.Length > 0 ? extension[1..].ToLowerInvariant() : string.Empty;
    }

    /// <summary>Formats a byte count as a human-readable size ("1.5 MB").</summary>
    public static string GetFileSize(long sizeInBytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = sizeInBytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    /// <summary>
    /// Generates a collision-resistant storage file name that preserves the
    /// original extension (e.g. "a3f1...-e9.pdf"), so the original,
    /// user-supplied name never ends up as a path/traversal or overwrite risk.
    /// </summary>
    public static string GenerateFileName(string originalFileName)
    {
        var extension = GetExtension(originalFileName);
        var uniqueName = $"{Guid.NewGuid():N}";
        return string.IsNullOrEmpty(extension) ? uniqueName : $"{uniqueName}.{extension}";
    }

    /// <summary>
    /// Tên file cho người dùng tải về, ví dụ "Users_20260801_1758.xlsx". Khác
    /// <see cref="GenerateFileName"/> ở chỗ tên phải đọc được: đây là file nằm
    /// trong thư mục Downloads của người dùng, không phải khoá lưu trữ.
    /// </summary>
    public static string GenerateTimestampedFileName(string prefix, string extension, DateTime? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var moment = timestamp ?? DateTime.UtcNow;
        return $"{prefix}_{moment:yyyyMMdd_HHmm}.{extension.TrimStart('.').ToLowerInvariant()}";
    }
}
