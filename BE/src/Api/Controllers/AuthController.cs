using Backend.Api.Extensions;
using Backend.Api.Middlewares;
using Backend.Application.DTOs.Auth;
using Backend.Application.Interfaces.Services;
using Backend.Application.Options;
using Backend.Infrastructure.Identity;
using Backend.Shared.Responses;
using Backend.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Backend.Api.Controllers;

/// <summary>
/// SCADA operator authentication against <c>scada.users</c>:
/// register, login, refresh, logout, me, change/forgot/reset password.
/// Concurrent limit is enforced at login; FE idle timeout calls logout (no heartbeat).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController(
    IAuthenticationService authenticationService,
    IConcurrentSessionService concurrentSessionService,
    IConcurrentLicenseService concurrentLicenseService,
    IOptions<LoginSecurityOptions> loginOptions) : ControllerBase
{
    private readonly LoginSecurityOptions _login = loginOptions.Value;

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var ip = ClientIp();
        var result = await authenticationService.RegisterAsync(request, ip, cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<AuthTokenResponse>.Ok(result.Value!, "Registration successful."));

        return MapAuthFailure(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceExtensions.LoginRateLimiterPolicy)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status423Locked)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(request, ClientIp(), cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<AuthTokenResponse>.Ok(result.Value!, "Login successful."));

        return MapAuthFailure(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RefreshAsync(request.RefreshToken, ClientIp(), cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<AuthTokenResponse>.Ok(result.Value!, "Token refreshed."));

        return MapAuthFailure(result);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [AllowWhenPasswordChangeRequired]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        // FE idle (normal users, 10 phút) hoặc logout chủ động → release Redis slot.
        // SessionId ưu tiên từ refresh token / JWT claim trong AuthenticationService.
        await authenticationService.LogoutAsync(request.RefreshToken, request.SessionId, ClientIp(), cancellationToken);
        return Ok(ApiResponse.Ok("Logged out."));
    }

    [HttpGet("concurrent-users")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ConcurrentUsersStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ConcurrentUsers(CancellationToken cancellationToken)
    {
        var status = await BuildStatusAsync(cancellationToken);
        return status.IsSuccess
            ? Ok(ApiResponse<ConcurrentUsersStatusDto>.Ok(status.Value!))
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail(status.Errors.FirstOrDefault() ?? ConcurrentSessionMessages.RedisUnavailable, status.Errors));
    }

    [HttpGet("me")]
    [AllowWhenPasswordChangeRequired]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CurrentUserResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await authenticationService.GetCurrentUserAsync(cancellationToken);

        if (result.IsFailure && result.ErrorCode is "Auth.Unauthorized")
            return Unauthorized(ApiResponse<CurrentUserResponse>.Fail(result.Errors.FirstOrDefault() ?? "Unauthorized.", result.Errors));

        return result.IsSuccess
            ? Ok(ApiResponse<CurrentUserResponse>.Ok(result.Value!))
            : NotFound(ApiResponse<CurrentUserResponse>.Fail(result.Errors.FirstOrDefault() ?? "User not found.", result.Errors));
    }

    [HttpPost("change-password")]
    [AllowWhenPasswordChangeRequired]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ChangePasswordAsync(request, cancellationToken);

        if (result.IsFailure && result.ErrorCode is "Auth.Unauthorized")
            return Unauthorized(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "Unauthorized.", result.Errors));

        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Password changed."))
            : BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "Password change failed.", result.Errors));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<ForgotPasswordResponse>.Ok(result.Value!));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.ResetPasswordAsync(request, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse.Ok("Password has been reset."))
            : BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? "Password reset failed.", result.Errors));
    }

    private async Task<Result<ConcurrentUsersStatusDto>> BuildStatusAsync(CancellationToken cancellationToken)
    {
        var limit = await concurrentLicenseService.GetEffectiveLimitAsync(cancellationToken);
        var statusResult = await concurrentSessionService.GetStatusAsync(cancellationToken);
        if (statusResult.IsFailure)
            return statusResult;

        var status = statusResult.Value!;
        status.MaxConcurrentUsers = limit.MaxConcurrentUsers;
        status.ReservedAdminSlots = limit.ReservedAdminSlots;
        status.MaxNormalConcurrentUsers = limit.MaxNormalConcurrentUsers;
        status.AvailableNormalSlots = Math.Max(0, limit.MaxNormalConcurrentUsers - status.ActiveNormalUsers);
        status.UsingLicense = limit.UsingLicense;
        status.TotalActiveUsers = status.ActiveAdminUsers + status.ActiveNormalUsers;
        return Result<ConcurrentUsersStatusDto>.Success(status);
    }

    private IActionResult MapAuthFailure(Backend.Shared.Results.Result result)
    {
        var message = result.Errors.FirstOrDefault() ?? "Request failed.";
        return result.ErrorCode switch
        {
            "Auth.ConcurrentLimit" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(message, result.Errors)),
            "Auth.ConcurrentUnavailable" => StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail(message, result.Errors)),
            "Auth.AccountLocked" => LockedResponse(message, result.Errors),
            "Auth.InvalidCredentials" or "Auth.InvalidRefreshToken" =>
                Unauthorized(ApiResponse<object>.Fail(message, result.Errors)),
            _ => BadRequest(ApiResponse<object>.Fail(message, result.Errors))
        };
    }

    // BE 1.4 — 423 Locked with a coarse Retry-After (max lock duration) so the
    // exact remaining lock time is not leaked.
    private IActionResult LockedResponse(string message, string[] errors)
    {
        Response.Headers.RetryAfter = (_login.LockMinutes * 60).ToString();
        return StatusCode(StatusCodes.Status423Locked, ApiResponse<object>.Fail(message, errors));
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
