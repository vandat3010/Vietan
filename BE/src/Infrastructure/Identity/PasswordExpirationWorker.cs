using Backend.Application.Common;
using Backend.Application.Options;
using Backend.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// BE 1.3đ — periodically flags users whose password has expired so the BE 1.3a
/// gate (<c>MustChangePassword</c>) blocks their business APIs. Login/refresh/me
/// also flag on demand, so the worker only closes the gap between expiry and the
/// user's next request. DB-side batch update: never loads the user table.
/// </summary>
public sealed class PasswordExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IPasswordExpirationPolicy policy,
    IOptions<PasswordPolicyOptions> options,
    ILogger<PasswordExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.ExpirationCheckIntervalMinutes));

        // Small initial delay so the worker never competes with app startup.
        try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a transient failure kill the BackgroundService.
                logger.LogWarning(ex, "Password expiration worker tick failed");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;
        var cutoff = policy.ExpirationCutoffUtc(now);

        // Idempotent + race-safe: only false→true, only truly-expired, DB-side.
        var affected = await db.ScadaUsers
            .Where(u => u.PasswordUpdatedAt != null
                        && u.PasswordUpdatedAt <= cutoff
                        && !u.MustChangePassword)
            .ExecuteUpdateAsync(
                s => s.SetProperty(u => u.MustChangePassword, true)
                      .SetProperty(u => u.UpdatedAt, now),
                cancellationToken);

        if (affected > 0)
            logger.LogInformation("Password expiration worker completed. Users marked for password change: {Count}", affected);
    }
}
