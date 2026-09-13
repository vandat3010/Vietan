using Backend.Application.Interfaces.Services;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Responses;
using Backend.Shared.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Controllers;

/// <summary>System license concurrent-user status (no sensitive license key returned).</summary>
[ApiController]
[Route("api/v1/licenses")]
[Produces("application/json")]
public class LicensesController(
    ApplicationDbContext db,
    IConcurrentSessionService concurrentSessionService,
    IConcurrentLicenseService concurrentLicenseService) : ControllerBase
{
    [HttpGet("concurrent-users")]
    [ProducesResponseType(typeof(ApiResponse<ConcurrentUsersStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetCurrentConcurrentUsers(CancellationToken cancellationToken)
    {
        var status = await BuildStatusAsync(cancellationToken);
        return status.IsSuccess
            ? Ok(ApiResponse<ConcurrentUsersStatusDto>.Ok(status.Value!))
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail(status.Errors.FirstOrDefault() ?? ConcurrentSessionMessages.RedisUnavailable, status.Errors));
    }

    [HttpGet("{id:guid}/concurrent-users")]
    [ProducesResponseType(typeof(ApiResponse<ConcurrentUsersStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetConcurrentUsers(Guid id, CancellationToken cancellationToken)
    {
        var exists = await db.SystemLicenses.AsNoTracking()
            .AnyAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);
        if (!exists)
            return NotFound(ApiResponse.Fail("License was not found."));

        var status = await BuildStatusAsync(cancellationToken);
        return status.IsSuccess
            ? Ok(ApiResponse<ConcurrentUsersStatusDto>.Ok(status.Value!))
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse.Fail(status.Errors.FirstOrDefault() ?? ConcurrentSessionMessages.RedisUnavailable, status.Errors));
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
}
