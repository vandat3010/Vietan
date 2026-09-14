using Backend.Application.Common;
using Backend.Application.DTOs.Licenses;
using Backend.Application.Interfaces.Services;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Constants;
using Backend.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Identity;

public sealed class SystemLicenseAdminService(
    ApplicationDbContext db,
    ICurrentUserService currentUser,
    ISystemAuditService systemAudit) : ISystemLicenseAdminService
{
    public async Task<Result<IReadOnlyList<SystemLicenseDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await db.SystemLicenses.AsNoTracking()
            .OrderByDescending(l => l.IsEnabled)
            .ThenByDescending(l => l.MaxConcurrentUsers)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<SystemLicenseDto>>.Success(items.Select(Map).ToList());
    }

    public async Task<Result<SystemLicenseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await db.SystemLicenses.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return row is null
            ? Result<SystemLicenseDto>.Failure("License.NotFound", "License was not found.")
            : Result<SystemLicenseDto>.Success(Map(row));
    }

    public async Task<Result<SystemLicenseDto>> CreateAsync(
        CreateSystemLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseKey) || request.LicenseKey.Trim().Length < 8)
            return Result<SystemLicenseDto>.Failure("ValidationError", "LicenseKey must be at least 8 characters.");

        if (request.MaxConcurrentUsers < 2)
            return Result<SystemLicenseDto>.Failure("ValidationError", "MaxConcurrentUsers must be at least 2.");

        if (request.ValidFrom is { } from && request.ValidTo is { } to && to < from)
            return Result<SystemLicenseDto>.Failure("ValidationError", "ValidTo must be >= ValidFrom.");

        var key = request.LicenseKey.Trim();
        var exists = await db.SystemLicenses.AnyAsync(l => l.LicenseKey == key, cancellationToken);
        if (exists)
            return Result<SystemLicenseDto>.Failure("License.Conflict", "License key already exists.");

        var entity = new SystemLicense
        {
            LicenseKey = key,
            MaxConcurrentUsers = request.MaxConcurrentUsers,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            IsEnabled = request.IsEnabled,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        db.SystemLicenses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        await systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.CreateLicense,
            EventType = AuditEventType.Configuration,
            Status = AuditStatus.Success,
            Module = "Licenses",
            EntityType = "SystemLicense",
            EntityId = entity.Id.ToString(),
            Description = $"Created license (max={entity.MaxConcurrentUsers}).",
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username
        }, cancellationToken);

        return Result<SystemLicenseDto>.Success(Map(entity));
    }

    public async Task<Result<SystemLicenseDto>> UpdateAsync(
        Guid id,
        UpdateSystemLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemLicenses.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (entity is null)
            return Result<SystemLicenseDto>.Failure("License.NotFound", "License was not found.");

        if (request.LicenseKey is not null)
        {
            var key = request.LicenseKey.Trim();
            if (key.Length < 8)
                return Result<SystemLicenseDto>.Failure("ValidationError", "LicenseKey must be at least 8 characters.");
            var taken = await db.SystemLicenses.AnyAsync(l => l.Id != id && l.LicenseKey == key, cancellationToken);
            if (taken)
                return Result<SystemLicenseDto>.Failure("License.Conflict", "License key already exists.");
            entity.LicenseKey = key;
        }

        if (request.MaxConcurrentUsers is { } max)
        {
            if (max < 2)
                return Result<SystemLicenseDto>.Failure("ValidationError", "MaxConcurrentUsers must be at least 2.");
            entity.MaxConcurrentUsers = max;
        }

        if (request.ValidFrom is not null) entity.ValidFrom = request.ValidFrom;
        if (request.ValidTo is not null) entity.ValidTo = request.ValidTo;
        if (request.IsEnabled is { } enabled) entity.IsEnabled = enabled;
        if (request.Description is not null)
            entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (entity.ValidFrom is { } from && entity.ValidTo is { } to && to < from)
            return Result<SystemLicenseDto>.Failure("ValidationError", "ValidTo must be >= ValidFrom.");

        await db.SaveChangesAsync(cancellationToken);

        await systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.UpdateLicense,
            EventType = AuditEventType.Configuration,
            Status = AuditStatus.Success,
            Module = "Licenses",
            EntityType = "SystemLicense",
            EntityId = entity.Id.ToString(),
            Description = $"Updated license (max={entity.MaxConcurrentUsers}, enabled={entity.IsEnabled}).",
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username
        }, cancellationToken);

        return Result<SystemLicenseDto>.Success(Map(entity));
    }

    public async Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemLicenses.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        if (entity is null)
            return Result.Failure("License.NotFound", "License was not found.");

        if (!entity.IsEnabled && entity.IsDeleted)
            return Result.Success();

        entity.IsEnabled = false;
        entity.MarkAsDeleted(currentUser.Username);
        await db.SaveChangesAsync(cancellationToken);

        await systemAudit.LogAsync(new SystemAuditEntry
        {
            Action = AuditActionNames.DeactivateLicense,
            EventType = AuditEventType.Configuration,
            Status = AuditStatus.Success,
            Module = "Licenses",
            EntityType = "SystemLicense",
            EntityId = entity.Id.ToString(),
            Description = "Deactivated license.",
            UserId = currentUser.OperatorUserId,
            UserName = currentUser.Username
        }, cancellationToken);

        return Result.Success();
    }

    private static SystemLicenseDto Map(SystemLicense l) => new()
    {
        Id = l.Id,
        LicenseKeyMasked = MaskKey(l.LicenseKey),
        MaxConcurrentUsers = l.MaxConcurrentUsers,
        ValidFrom = l.ValidFrom,
        ValidTo = l.ValidTo,
        IsEnabled = l.IsEnabled,
        Description = l.Description,
        CreatedDate = l.CreatedDate,
        ModifiedDate = l.ModifiedDate
    };

    private static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (key.Length <= 8) return new string('*', key.Length);
        return $"{key[..4]}…{key[^4..]}";
    }
}
