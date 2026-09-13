using System.Security.Cryptography;
using System.Text;
using Backend.Domain.Interfaces;
using Isopoh.Cryptography.Argon2;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// Argon2id password hasher. Output is the PHC string
/// (<c>$argon2id$v=19$m=...,t=...,p=...$salt$hash</c>) so salt and parameters
/// travel with the hash — no separate salt column.
/// </summary>
public class Argon2idPasswordHasher(IOptions<PasswordHashingSettings> options) : IPasswordHasher
{
    private readonly PasswordHashingSettings _settings = options.Value;

    public string Hash(string plainPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);

        var salt = RandomNumberGenerator.GetBytes(_settings.SaltLength);
        var config = CreateConfig(Encoding.UTF8.GetBytes(plainPassword), salt);
        return Argon2.Hash(config);
    }

    public bool Verify(string plainPassword, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        // Legacy / seed placeholders (BCrypt, "TEST_HASH_...") are not Argon2id —
        // never try to "upgrade by decrypting"; just reject until the user resets.
        if (!passwordHash.StartsWith("$argon2", StringComparison.Ordinal))
            return false;

        try
        {
            return Argon2.Verify(passwordHash, plainPassword);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private Argon2Config CreateConfig(byte[] passwordBytes, byte[] salt) => new()
    {
        Type = Argon2Type.HybridAddressing, // Argon2id
        Version = Argon2Version.Nineteen,
        TimeCost = Math.Max(1, _settings.Iterations),
        MemoryCost = Math.Max(8 * Math.Max(1, _settings.DegreeOfParallelism), _settings.MemorySize),
        Lanes = Math.Max(1, _settings.DegreeOfParallelism),
        Threads = Math.Clamp(_settings.DegreeOfParallelism, 1, Environment.ProcessorCount),
        Password = passwordBytes,
        Salt = salt,
        HashLength = Math.Max(16, _settings.HashLength)
    };
}
