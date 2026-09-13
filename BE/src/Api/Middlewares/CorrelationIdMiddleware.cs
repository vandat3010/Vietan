using Serilog.Context;

namespace Backend.Api.Middlewares;

/// <summary>
/// Ensures every request has a correlation id (reusing an inbound
/// <c>X-Correlation-Id</c> header when the caller already supplied one, e.g. from
/// an upstream gateway/service), echoes it back on the response, and pushes it
/// into Serilog's LogContext so every log line for this request can be
/// correlated - see Infrastructure.Logging.SerilogConfiguration's output template.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
