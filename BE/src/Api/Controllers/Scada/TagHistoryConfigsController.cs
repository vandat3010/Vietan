using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API cấu hình tag ↔ history profile (scada.tag_history_config).</summary>
[ApiController]
[Route("api/v1/tag-history-configs")]
[Produces("application/json")]
public class TagHistoryConfigsController(ITagHistoryConfigQueryService configs) : ControllerBase
{
    /// <summary>
    /// Danh sách cấu hình ghi lịch sử theo tag.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<TagHistoryConfigDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<TagHistoryConfigDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] TagHistoryConfigQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await configs.GetPagedAsync(query, cancellationToken), "Danh sách tag-history-config.");

    /// <summary>
    /// Chi tiết 1 tag-history-config theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TagHistoryConfigDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<TagHistoryConfigDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TagHistoryConfigDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await configs.GetByIdAsync(id, cancellationToken), "Chi tiết tag-history-config.");
}
