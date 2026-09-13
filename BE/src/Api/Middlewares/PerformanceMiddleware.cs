using System.Diagnostics;

namespace Backend.Api.Middlewares;

/// <summary>
/// Logs a warning for any request that takes longer than <see cref="SlowRequestThresholdMs"/>,
/// which is invaluable for spotting N+1 queries or missing indexes in a
/// reporting-heavy backend (ERP/MES dashboards) before users complain.
/// </summary>
public class PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
{
    private const int SlowRequestThresholdMs = 1000;

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow request: {Method} {Path} took {ElapsedMilliseconds}ms (status {StatusCode})",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
    }
}
