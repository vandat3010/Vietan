using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API user SCADA nội bộ (scada.scada_user) — Admin only.</summary>
[ApiController]
[Route("api/v1/scada-users")]
[Produces("application/json")]
[Authorize(Roles = ScadaRoles.AdminOnly)]
public class ScadaUsersController(IScadaUserQueryService users) : ControllerBase
{
    /// <summary>
    /// Danh sách SCADA user.
    /// Status : 200 OK | 401 | 403 | 500
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScadaUserDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScadaUserDto>>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScadaUserDto>>), ScadaHttpStatuses.Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScadaUserDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] ScadaUserQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await users.GetPagedAsync(query, cancellationToken), "Danh sách SCADA user.");

    /// <summary>
    /// Chi tiết 1 SCADA user theo Id.
    /// Status : 200 OK | 401 | 403 | 404 | 500
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await users.GetByIdAsync(id, cancellationToken), "Chi tiết SCADA user.");

    /// <summary>
    /// Tạo SCADA user (màn Thêm người dùng mới). Không phát JWT.
    /// Status : 200 OK | 400 | 401 | 403 | 409 | 500
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Conflict)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateScadaUserRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await users.CreateAsync(request, cancellationToken), "Tạo SCADA user thành công.");

    /// <summary>
    /// Cập nhật hồ sơ SCADA user (không đổi mật khẩu / username).
    /// Status : 200 OK | 400 | 401 | 403 | 404 | 409 | 500
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Conflict)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Forbidden)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateScadaUserRequest request,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await users.UpdateAsync(id, request, cancellationToken), "Cập nhật SCADA user thành công.");

    /// <summary>
    /// Soft-deactivate user (<c>IsActive = false</c>) — giữ lịch sử audit/FK.
    /// Status : 204 No Content | 401 | 403 | 404 | 500
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(ScadaHttpStatuses.NoContent)]
    [ProducesResponseType(typeof(ApiResponse), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse), ScadaHttpStatuses.Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), ScadaHttpStatuses.Unauthorized)]
    public async Task<IActionResult> Deactivate(long id, CancellationToken cancellationToken) =>
        this.ToNoContentResult(await users.DeactivateAsync(id, cancellationToken));
}
