using Backend.Application.DTOs.Map;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

public interface IMapLayerService
{
    Task<Result<IReadOnlyList<MapLayerDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<MapLayerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<MapLayerDto>> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        MapLayerMetaDto? meta,
        CancellationToken cancellationToken = default);

    Task<Result<MapLayerMetaDto>> UpdateMetaAsync(Guid id, UpdateMapLayerRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<(Stream Stream, string FileName, string ContentType)>> OpenFileAsync(Guid id, CancellationToken cancellationToken = default);
}
