using Backend.Application.DTOs.Reports;
using Backend.Application.Interfaces.Dapper;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>
/// Sample dashboard/report endpoints. Notice this controller depends directly on
/// <see cref="IUserReportQueries"/> (Dapper) instead of <c>IUserService</c>
/// (EF Core) - this is the intended pattern for any read-heavy, reporting-style
/// endpoint (ERP/MES/SCADA dashboards). See README "Why EF Core AND Dapper?".
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.Reports.View)]
public class ReportsController(IUserReportQueries userReportQueries) : ControllerBase
{
    [HttpGet("users/summary")]
    [ProducesResponseType(typeof(ApiResponse<UserSummaryReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSummary(CancellationToken cancellationToken)
    {
        var summary = await userReportQueries.GetUserSummaryAsync(cancellationToken);
        return Ok(ApiResponse<UserSummaryReportDto>.Ok(summary));
    }

    [HttpGet("users/top-active")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserActivityReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopActiveUsers([FromQuery] int top = 10, CancellationToken cancellationToken = default)
    {
        var users = await userReportQueries.GetTopActiveUsersAsync(top, cancellationToken);
        return Ok(ApiResponse<IEnumerable<UserActivityReportDto>>.Ok(users));
    }
}
