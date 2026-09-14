using Backend.Application.DTOs.Licenses;
using Backend.Application.Interfaces.Services;
using Backend.Infrastructure.Identity;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Backend.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>System license admin + concurrent-user status. Never returns full license key.</summary>
[ApiController]
[Route("api/v1/licenses")]
[Produces("application/json")]
[Authorize(Roles = ScadaRoles.AdminOnly)]
public class LicensesController(
    ISystemLicenseAdminService licenses,
    IConcurrentSessionService concurrentSessionService,
    IConcurrentLicenseService concurrentLicenseService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SystemLicenseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await licenses.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SystemLicenseDto>>.Ok(result.Value!, "Danh sách license."));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await licenses.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(ApiResponse<SystemLicenseDto>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
        return Ok(ApiResponse<SystemLicenseDto>.Ok(result.Value!, "Chi tiết license."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSystemLicenseRequest request, CancellationToken cancellationToken)
    {
        var result = await licenses.CreateAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            var body = ApiResponse<SystemLicenseDto>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors);
            if (result.ErrorCode.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
                return Conflict(body);
            return BadRequest(body);
        }

        return Ok(ApiResponse<SystemLicenseDto>.Ok(result.Value!, "Created license."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSystemLicenseRequest request, CancellationToken cancellationToken)
    {
        var result = await licenses.UpdateAsync(id, request, cancellationToken);
        if (result.IsFailure)
        {
            var body = ApiResponse<SystemLicenseDto>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors);
            if (result.ErrorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                return NotFound(body);
            if (result.ErrorCode.Contains("Conflict", StringComparison.OrdinalIgnoreCase))
                return Conflict(body);
            return BadRequest(body);
        }

        return Ok(ApiResponse<SystemLicenseDto>.Ok(result.Value!, "Updated license."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await licenses.DeactivateAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            if (result.ErrorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                return NotFound(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
            return BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
        }

        return NoContent();
    }

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
    public async Task<IActionResult> GetConcurrentUsers(Guid id, CancellationToken cancellationToken)
    {
        var license = await licenses.GetByIdAsync(id, cancellationToken);
        if (license.IsFailure)
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
