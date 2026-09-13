using Backend.Application.Common;
using Backend.Application.Interfaces.Services;
using Backend.Domain.Entities.Scada;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Persistence.Auditing;

/// <summary>
/// BE 3.1a — fail-safe System Audit writer.
/// <list type="bullet">
/// <item>Persists on an ISOLATED DbContext scope so the audit row survives even
/// when the originating request's transaction has already failed (error path).</item>
/// <item>NEVER throws: an audit failure is logged to the application log only, so it
/// cannot break the business flow (§15).</item>
/// <item>Enriches missing actor/context from the current request; secrets in
/// <c>AdditionalData</c> are redacted before persistence (§3/§26).</item>
/// </list>
/// Writes are synchronous (durable for security-critical events like Login). The
/// call sites are low-frequency (login/export/error), so this is not on the hot
/// realtime path; a bounded background queue can be introduced later if needed.
/// </summary>
public sealed class SystemAuditService(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SystemAuditService> logger) : ISystemAuditService
{
    // Kept as a literal (Infrastructure must not depend on the Api layer). Must match
    // Backend.Api.Middlewares.CorrelationIdMiddleware.HeaderName.
    private const string CorrelationHeader = "X-Correlation-Id";

    public async Task LogAsync(SystemAuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        try
        {
            var http = httpContextAccessor.HttpContext;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currentUser = scope.ServiceProvider.GetService<ICurrentUserService>();

            var now = DateTimeOffset.UtcNow;
            var log = new SystemAuditLog
            {
                Action = entry.Action,
                EventType = entry.EventType,
                Status = entry.Status,
                Description = AuditSanitizer.Truncate(entry.Description),
                Module = entry.Module,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                HttpStatusCode = entry.HttpStatusCode,
                UserId = entry.UserId ?? currentUser?.OperatorUserId,
                UserName = entry.UserName ?? currentUser?.Username,
                IpAddress = entry.IpAddress ?? currentUser?.IpAddress ?? http?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = AuditSanitizer.Truncate(entry.UserAgent ?? ResolveUserAgent(http), 512),
                Endpoint = entry.Endpoint ?? http?.Request.Path.Value,
                HttpMethod = entry.HttpMethod ?? http?.Request.Method,
                CorrelationId = entry.CorrelationId ?? ResolveCorrelationId(http),
                AdditionalData = SerializeAdditionalData(entry.AdditionalData),
                CreatedAt = now,
                UpdatedAt = now
            };

            db.SystemAuditLogs.Add(log);

            // Dual-write ERD UserActivityLogs (history) for operator/security trail.
            // Failures here must not break SystemAudit; same isolated scope.
            db.UserActivityLogs.Add(new Domain.Entities.History.UserActivityLog
            {
                CreatedAt = now,
                UserId = log.UserId,
                Username = log.UserName,
                Role = currentUser?.Roles is { Count: > 0 } roles ? string.Join(',', roles) : null,
                ActionType = log.Action,
                Module = log.Module,
                Description = log.Description,
                IpAddress = log.IpAddress,
                Status = log.Status.ToString()
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to write system audit log (Action={Action}, EventType={EventType}, Status={Status})",
                entry.Action, entry.EventType, entry.Status);
        }
    }

    private static string? ResolveUserAgent(HttpContext? http) =>
        http?.Request.Headers["User-Agent"].ToString() is { Length: > 0 } ua ? ua : null;

    private static string? ResolveCorrelationId(HttpContext? http)
    {
        if (http is null)
            return null;

        if (http.Items.TryGetValue(CorrelationHeader, out var item) && item is string s && !string.IsNullOrWhiteSpace(s))
            return s;

        return http.Response.Headers.TryGetValue(CorrelationHeader, out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : null;
    }

    private static string? SerializeAdditionalData(IReadOnlyDictionary<string, object?>? data)
    {
        if (data is null || data.Count == 0)
            return null;

        var sanitized = AuditSanitizer.Sanitize(data);
        return sanitized.Count == 0 ? null : JsonHelper.Serialize(sanitized);
    }
}
