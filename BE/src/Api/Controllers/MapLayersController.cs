using System.Text.Json;
using Backend.Application.DTOs.Map;
using Backend.Application.Interfaces.Services;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

/// <summary>Map layers (KMZ/KML) — metadata in DB, files via IFileStorageService.</summary>
[ApiController]
[Route("api/v1/map-layers")]
[Produces("application/json")]
[Authorize]
public class MapLayersController(IMapLayerService mapLayers) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MapLayerDto>>), ScadaHttpStatuses.Ok)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mapLayers.GetAllAsync(cancellationToken);
        return result.IsSuccess
            ? Ok(ApiResponse<IReadOnlyList<MapLayerDto>>.Ok(result.Value!, "Danh sách map layer."))
            : BadRequest(ApiResponse<IReadOnlyList<MapLayerDto>>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mapLayers.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(ApiResponse<MapLayerDto>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
        return Ok(ApiResponse<MapLayerDto>.Ok(result.Value!, "Chi tiết map layer."));
    }

    [HttpGet("{id:guid}/file")]
    [ProducesResponseType(ScadaHttpStatuses.Ok)]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken cancellationToken)
    {
        var result = await mapLayers.OpenFileAsync(id, cancellationToken);
        if (result.IsFailure)
            return NotFound(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));

        var (stream, fileName, contentType) = result.Value!;
        return File(stream, contentType, fileName);
    }

    [HttpPost]
    [Authorize(Roles = ScadaRoles.AdminOnly)]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string? meta,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<MapLayerDto>.Fail("File is required."));

        MapLayerMetaDto? metaDto = null;
        if (!string.IsNullOrWhiteSpace(meta))
        {
            try
            {
                metaDto = JsonSerializer.Deserialize<MapLayerMetaDto>(meta, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return BadRequest(ApiResponse<MapLayerDto>.Fail("Invalid meta JSON."));
            }
        }

        metaDto ??= new MapLayerMetaDto
        {
            Name = Request.Form["name"].ToString(),
            FileName = Request.Form["fileName"].ToString(),
            Color = Request.Form["color"].ToString(),
            Visible = !bool.TryParse(Request.Form["visible"], out var vis) || vis,
            Opacity = double.TryParse(Request.Form["opacity"], out var op) ? op : 1,
            Weight = double.TryParse(Request.Form["weight"], out var w) ? w : 2,
            Id = Request.Form["id"].ToString()
        };

        await using var stream = file.OpenReadStream();
        var result = await mapLayers.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            metaDto,
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<MapLayerDto>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));

        return Ok(ApiResponse<MapLayerDto>.Ok(result.Value!, "Uploaded map layer."));
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = ScadaRoles.AdminOnly)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] UpdateMapLayerRequest request, CancellationToken cancellationToken)
    {
        var result = await mapLayers.UpdateMetaAsync(id, request, cancellationToken);
        if (result.IsFailure)
        {
            if (result.ErrorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                return NotFound(ApiResponse<MapLayerMetaDto>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
            return BadRequest(ApiResponse<MapLayerMetaDto>.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
        }

        return Ok(ApiResponse<MapLayerMetaDto>.Ok(result.Value!, "Updated map layer."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = ScadaRoles.AdminOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mapLayers.DeleteAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            if (result.ErrorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
                return NotFound(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
            return BadRequest(ApiResponse.Fail(result.Errors.FirstOrDefault() ?? result.ErrorCode, result.Errors));
        }

        return NoContent();
    }
}
