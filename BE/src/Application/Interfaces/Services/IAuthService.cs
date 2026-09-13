using System.Security.Claims;
using Backend.Application.DTOs.Auth;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

/// <summary>
/// The authentication workflow: issuing, rotating and invalidating credentials.
/// <para>
/// Named IAuthService (not IAuthenticationService) because it already existed
/// under this name and a second interface with the same responsibility would give
/// the solution two front doors to the same behaviour.
/// </para>
/// <para>
/// Self-service password change deliberately lives on
/// <see cref="IUserService.ChangePasswordAsync"/>: it operates on a user profile
/// the caller already owns, and duplicating it here would mean two code paths
/// enforcing the same password policy.
/// </para>
/// </summary>
public interface IAuthService
{
    Task<Result<AuthResultDto>> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);

    Task<Result<AuthResultDto>> LoginAsync(LoginDto request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<Result<AuthResultDto>> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Revokes one refresh token (single device/session).</summary>
    Task<Result> RevokeTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes ALL of the user's refresh tokens. Access tokens already issued stay
    /// valid until they expire - that is inherent to stateless JWT, and the reason
    /// access-token lifetimes are kept short.
    /// </summary>
    Task<Result> LogoutAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Administrative reset: sets a new password for another account and kills its
    /// sessions. The caller (controller/policy) is responsible for authorising it.
    /// The self-service "forgot password" flow needs a mailed, single-use token -
    /// that token store and its email sender are intentionally left as an
    /// extension point rather than half-implemented here.
    /// </summary>
    Task<Result> ResetPasswordAsync(ResetPasswordDto request, CancellationToken cancellationToken = default);

    /// <summary>Validates an access token's signature/lifetime and returns its claims, or a failure Result.</summary>
    Task<Result<ClaimsPrincipal>> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}
