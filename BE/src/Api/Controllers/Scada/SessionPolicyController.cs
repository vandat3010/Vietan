using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>Cấu hình phiên làm việc (idle timeout) — lưu <c>app.app_settings</c>.</summary>
[ApiController]
[Route("api/v1/session-policy")]
[Produces("application/json")]
[Authorize]
public class SessionPolicyController(IAppSettingQueryService settings) : ControllerBase
{
    /// <summary>Lấy idle timeout (phút). Missing/invalid → default an toàn.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SessionPolicyDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<SessionPolicyDto>), ScadaHttpStatuses.Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        this.ToActionResult(
            await settings.GetSessionPolicyAsync(cancellationToken),
            "Cấu hình phiên làm việc.");

    /// <summary>Cập nhật idle timeout (phút) — Admin only.</summary>
    [HttpPut]
    [Authorize(Roles = ScadaRoles.AdminOnly)]
    [ProducesResponseType(typeof(ApiResponse<SessionPolicyDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<SessionPolicyDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionPolicyDto>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SessionPolicyDto>), ScadaHttpStatuses.Forbidden)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateSessionPolicyRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(
            await settings.UpdateSessionPolicyAsync(request, cancellationToken),
            "Đã lưu cấu hình phiên làm việc.");
}
