using System.Security.Claims;
using Backend.Domain.Entities;

namespace Backend.Application.Interfaces.Services;

/// <summary>
/// Issues/validates JWT access tokens and opaque refresh tokens.
/// Implemented in Infrastructure.Identity.JwtTokenService (needs signing-key
/// configuration, which is an infrastructure concern).
/// </summary>
public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAtUtc) GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();

    /// <summary>Full validation including lifetime; returns null when the token is invalid or expired.</summary>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Validates everything EXCEPT the lifetime. Only for the refresh flow, where
    /// the access token is expected to be expired but must still prove it was
    /// issued by us.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
