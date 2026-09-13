namespace Backend.Domain.Interfaces;

/// <summary>
/// Domain service abstraction: hashing a password is a domain concern (the User
/// aggregate decides *when* a password may change) but the actual algorithm
/// (BCrypt/Argon2/etc.) is an infrastructure detail, so only the interface lives here.
/// Implemented by Backend.Infrastructure.Identity.Argon2idPasswordHasher.
/// There is no decode/decrypt — only Hash and Verify.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string passwordHash);
}
