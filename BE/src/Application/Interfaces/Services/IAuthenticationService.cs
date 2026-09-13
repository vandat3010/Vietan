using System.Security.Claims;
using Backend.Application.DTOs.Auth;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

/// <summary>
/// Authentication for SCADA operators (<c>scada.users</c>): register/login,
/// JWT + refresh rotation, password change/reset. Role is always read from
/// <c>Users.Role</c> — never hard-coded.
/// </summary>
public interface IAuthenticationService
{
    Task<Result<AuthTokenResponse>> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<Result<AuthTokenResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    Task<Result<AuthTokenResponse>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(string refreshToken, string? sessionIdHint, string? ipAddress, CancellationToken cancellationToken = default);

    Task<Result<CurrentUserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<Result<ClaimsPrincipal>> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}
