using Backend.Application.Common;
using Backend.Application.DTOs.Map;
using Backend.Application.Interfaces.Services;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Constants;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Scada;

public sealed class MapLayerService(
    ApplicationDbContext db,
    IFileStorageService fileStorage,
    ICurrentUserService currentUser,
    ISystemAuditService systemAudit) : IMapLayerService
{
    private const string Folder = "map-layers";
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".kmz", ".kml"
    };

    public async Task<Result<IReadOnlyList<MapLayerDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.MapLayers.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var list = new List<MapLayerDto>(rows.Count);
        foreach (var row in rows)
            list.Add(await ToDtoAsync(row, cancellationToken));

        return Result<IReadOnlyList<MapLayerDto>>.Success(list);
    }

    public async Task<Result<MapLayerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await db.MapLayers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row is null
            ? Result<MapLayerDto>.Failure("MapLayer.NotFound", "Map layer was not found.")
            : Result<MapLayerDto>.Success(await ToDtoAsync(row, cancellationToken));
    }

    public async Task<Result<MapLayerDto>> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        MapLayerMetaDto? meta,
        CancellationToken cancellationToken = default)
    {
        if (content is null || content == Stream.Null)
            return Result<MapLayerDto>.Failure("ValidationError", "File is required.");

        var safeName = Path.GetFileName(fileName);
        var ext = Path.GetExtension(safeName);
        if (!AllowedExtensions.Contains(ext))
            return Result<MapLayerDto>.Failure("ValidationError", "Only .kmz / .kml files are allowed.");

        var upload = await fileStorage.UploadAsync(content, safeName, contentType, Folder, cancellationToken);

        var id = Guid.NewGuid();
        if (meta?.Id is { Length: > 0 } rawId && Guid.TryParse(rawId, out var guidFromMeta))
            id = guidFromMeta;

        var entity = new MapLayer(id)
        {
            Name = string.IsNullOrWhiteSpace(meta?.Name) ? Path.GetFileNameWithoutExtension(safeName) : meta!.Name.Trim(),
            FileName = string.IsNullOrWhiteSpace(meta?.FileName) ? safeName : meta!.FileName.Trim(),
            StoragePath = upload.StoragePath,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            Opacity = Clamp(meta?.Opacity ?? 1, 0, 1),
            Weight = Math.Max(0, meta?.Weight ?? 2),
            Visible = meta?.Visible ?? true,
            Color = string.IsNullOrWhiteSpace(meta?.Color) ? "#3388ff" : meta!.Color.Trim(),
            SortOrder = 0
        };

        db.MapLayers.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        await systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.CreateMapLayer,
            EventType = AuditEventType.Configuration,
            Status = AuditStatus.Success,
            Module = "MapLayers",
            EntityType = "MapLayer",
            EntityId = entity.Id.ToString(),
            Description = $"Uploaded map layer '{entity.Name}'.",
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username
        }, cancellationToken);

        return Result<MapLayerDto>.Success(await ToDtoAsync(entity, cancellationToken));
    }

    public async Task<Result<MapLayerMetaDto>> UpdateMetaAsync(
        Guid id,
        UpdateMapLayerRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.MapLayers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return Result<MapLayerMetaDto>.Failure("MapLayer.NotFound", "Map layer was not found.");

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (name.Length == 0)
                return Result<MapLayerMetaDto>.Failure("ValidationError", "Name cannot be empty.");
            entity.Name = name;
        }

        if (request.Opacity is { } opacity)
            entity.Opacity = Clamp(opacity, 0, 1);
        if (request.Weight is { } weight)
            entity.Weight = Math.Max(0, weight);
        if (request.Visible is { } visible)
            entity.Visible = visible;
        if (request.Color is not null)
            entity.Color = string.IsNullOrWhiteSpace(request.Color) ? entity.Color : request.Color.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.UpdateMapLayer,
            EventType = AuditEventType.Configuration,
            Status = AuditStatus.Success,
            Module = "MapLayers",
            EntityType = "MapLayer",
            EntityId = entity.Id.ToString(),
            Description = $"Updated map layer '{entity.Name}'.",
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username
        }, cancellationToken);

        return Result<MapLayerMetaDto>.Success(ToMeta(entity));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.MapLayers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            return Result.Failure("MapLayer.NotFound", "Map layer was not found.");

        var path = entity.StoragePath;
        entity.MarkAsDeleted(currentUser.Username);
        await db.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(path, cancellationToken);

        await systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.DeleteMapLayer,
            EventType = AuditEventType.Configuration,
            Status = AuditStatus.Success,
            Module = "MapLayers",
            EntityType = "MapLayer",
            EntityId = id.ToString(),
            Description = $"Deleted map layer '{entity.Name}'.",
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username
        }, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<(Stream Stream, string FileName, string ContentType)>> OpenFileAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await db.MapLayers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null)
            return Result<(Stream, string, string)>.Failure("MapLayer.NotFound", "Map layer was not found.");

        var stream = await fileStorage.DownloadAsync(row.StoragePath, cancellationToken);
        return Result<(Stream, string, string)>.Success((stream, row.FileName, row.ContentType));
    }

    private async Task<MapLayerDto> ToDtoAsync(MapLayer row, CancellationToken cancellationToken)
    {
        var url = await fileStorage.GetFileUrlAsync(row.StoragePath, cancellationToken: cancellationToken);
        // Prefer API file route for auth-gated download.
        var fileUrl = $"/api/v1/map-layers/{row.Id}/file";
        _ = url;
        return new MapLayerDto
        {
            Id = row.Id.ToString(),
            Meta = ToMeta(row),
            FileUrl = fileUrl
        };
    }

    private static MapLayerMetaDto ToMeta(MapLayer row) => new()
    {
        Id = row.Id.ToString(),
        Name = row.Name,
        FileName = row.FileName,
        Opacity = row.Opacity,
        Weight = row.Weight,
        Visible = row.Visible,
        Color = row.Color
    };

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : value > max ? max : value;
}
