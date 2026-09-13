using Backend.Application.Interfaces.Services;
using Backend.Application.Options;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Identity;

public class ConcurrentLicenseService(
    ApplicationDbContext db,
    IOptions<ConcurrentSessionOptions> options,
    ILogger<ConcurrentLicenseService> logger) : IConcurrentLicenseService
{
    public async Task<ConcurrentLimitInfo> GetEffectiveLimitAsync(CancellationToken cancellationToken = default)
    {
        var cfg = options.Value;
        var reserved = Math.Max(0, cfg.ReservedAdminSlots);
        var defaultMax = Math.Max(reserved + 1, cfg.DefaultMaxConcurrentUsers);

        var now = DateTimeOffset.UtcNow;
        var license = await db.SystemLicenses.AsNoTracking()
            .Where(l => !l.IsDeleted
                        && l.IsEnabled
                        && (l.ValidFrom == null || l.ValidFrom <= now)
                        && (l.ValidTo == null || l.ValidTo >= now)
                        && l.MaxConcurrentUsers > 0)
            .OrderByDescending(l => l.MaxConcurrentUsers)
            .ThenByDescending(l => l.ModifiedDate ?? l.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (license is null)
        {
            return new ConcurrentLimitInfo
            {
                MaxConcurrentUsers = defaultMax,
                ReservedAdminSlots = reserved,
                UsingLicense = false
            };
        }

        if (license.MaxConcurrentUsers < reserved + 1)
        {
            logger.LogWarning(
                "License {LicenseId} maxConcurrentUsers={Max} is below reservedAdminSlots+1; clamping to default {Default}",
                license.Id, license.MaxConcurrentUsers, defaultMax);
            return new ConcurrentLimitInfo
            {
                MaxConcurrentUsers = defaultMax,
                ReservedAdminSlots = reserved,
                ActiveLicenseId = license.Id,
                UsingLicense = false
            };
        }

        logger.LogInformation(
            "Using license {LicenseId} maxConcurrentUsers={Max}",
            license.Id, license.MaxConcurrentUsers);

        return new ConcurrentLimitInfo
        {
            MaxConcurrentUsers = license.MaxConcurrentUsers,
            ReservedAdminSlots = reserved,
            ActiveLicenseId = license.Id,
            UsingLicense = true
        };
    }

    public static bool IsAdminRole(string? role) =>
        string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase);
}
