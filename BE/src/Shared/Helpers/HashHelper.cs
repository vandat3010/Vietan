using System.Security.Cryptography;
using System.Text;

namespace Backend.Shared.Helpers;

/// <summary>
/// One-way SHA-256 hashing for values that must be compared but never need to be
/// reversed (e.g. hashing a refresh token before storing it, or fingerprinting a
/// file for de-duplication). NOT for passwords - see
/// <c>Backend.Domain.Interfaces.IPasswordHasher</c> (BCrypt, salted, slow-by-design)
/// for that; SHA-256 alone is unsuitable for password storage.
/// </summary>
public static class HashHelper
{
    public static string Sha256(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    public static bool VerifyHash(string value, string expectedHash) =>
        string.Equals(Sha256(value), expectedHash, StringComparison.OrdinalIgnoreCase);
}
