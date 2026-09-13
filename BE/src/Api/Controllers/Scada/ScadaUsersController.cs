using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API user SCADA nội bộ (scada.scada_user).</summary>
[ApiController]
[Route("api/v1/scada-users")]
[Produces("application/json")]
public class ScadaUsersController(IScadaUserQueryService users) : ControllerBase
{
    /// <summary>
    /// Danh sách SCADA user.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScadaUserDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<ScadaUserDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] ScadaUserQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await users.GetPagedAsync(query, cancellationToken), "Danh sách SCADA user.");

    /// <summary>
    /// Chi tiết 1 SCADA user theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await users.GetByIdAsync(id, cancellationToken), "Chi tiết SCADA user.");

    /// <summary>
    /// Tạo SCADA user (màn Thêm người dùng mới). Không phát JWT.
    /// Status : 200 OK | 400 Validation | 500 Lỗi không mong đợi
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ScadaUserDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateScadaUserRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await users.CreateAsync(request, cancellationToken), "Tạo SCADA user thành công.");
}
