using Backend.Application.DTOs.Scada;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Shared.Constants;
using Backend.Shared.Pagination;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>API tag (scada.tag).</summary>
[ApiController]
[Route("api/v1/tags")]
[Produces("application/json")]
public class TagsController(ITagQueryService tags) : ControllerBase
{
    /// <summary>
    /// Danh sách tag.
    /// Status : 200 OK | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<TagDto>>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<PaginationResult<TagDto>>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] TagQuery query, CancellationToken cancellationToken) =>
        this.ToActionResult(await tags.GetPagedAsync(query, cancellationToken), ScadaApiMessages.TagsListOk);

    /// <summary>
    /// Chi tiết 1 tag theo Id.
    /// Status : 200 OK | 404 Không tìm thấy | 500 Lỗi không mong đợi
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), ScadaHttpStatuses.Ok)]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), ScadaHttpStatuses.NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TagDto>), ScadaHttpStatuses.InternalServerError)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        this.ToActionResult(await tags.GetByIdAsync(id, cancellationToken), ScadaApiMessages.TagDetailOk);
}
