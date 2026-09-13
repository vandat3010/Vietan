using Backend.Domain.Entities.Scada;

namespace Backend.Application.Interfaces.Services;

/// <summary>Issues JWT access tokens for <see cref="ScadaUser"/> (dynamic role claim).</summary>
public interface IScadaTokenService
{
    (string AccessToken, DateTimeOffset ExpiresAt) CreateAccessToken(ScadaUser user, string sessionId);

    /// <summary>Cryptographically random opaque refresh token (plaintext — hash before store).</summary>
    string CreateRefreshToken();

    /// <summary>Cryptographically random password-reset token (plaintext — hash before store).</summary>
    string CreatePasswordResetToken();

    System.Security.Claims.ClaimsPrincipal? ValidateAccessToken(string token);
}
