using Backend.Domain.Interfaces;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// BCrypt-based implementation of the domain's <see cref="IPasswordHasher"/> contract.
/// Work factor 12 balances security vs. login latency; bump it as hardware improves.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: WorkFactor);

    public bool Verify(string plainPassword, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
}
