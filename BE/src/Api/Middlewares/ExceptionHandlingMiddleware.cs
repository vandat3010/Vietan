using System.Net;
using System.Text.Json;
using Backend.Application.Common;
using Backend.Application.Interfaces.Services;
using Backend.Domain.Enums;
using Backend.Shared.Responses;
using AppValidationException = Backend.Shared.Exceptions.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace Backend.Api.Middlewares;

/// <summary>
/// Single place where every unhandled exception in the pipeline is caught and
/// translated into a consistent <see cref="ErrorResponse"/> JSON body. Controllers
/// (and Application services, and Domain entities) never need try/catch blocks
/// for these known exception types - they just throw a
/// <c>Backend.Shared.Exceptions.*</c> type, and this middleware maps it to the
/// right HTTP status code.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env,
    ISystemAuditService auditService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var id)
            ? id?.ToString()
            : context.TraceIdentifier;

        var (statusCode, message, errorCode, errors) = MapException(exception);

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path} (CorrelationId: {CorrelationId})",
                context.Request.Method, context.Request.Path, correlationId);
        else
            logger.LogWarning("Handled exception {ExceptionType} for {Method} {Path}: {Message}",
                exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);

        // BE 3.1a — single owner of API/System error auditing (no per-controller
        // duplication). Only security-relevant/system failures are audited (§4/§16);
        // full stack traces stay in the application log, never in the audit DB (§10/§28).
        await TryAuditErrorAsync(context, exception, statusCode, message, correlationId);

        var response = new ErrorResponse
        {
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
            TraceId = correlationId,
            StatusCode = statusCode,
            Path = context.Request.Path,
#if DEBUG
            StackTrace = env.IsDevelopment() ? exception.ToString() : null
#endif
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private async Task TryAuditErrorAsync(HttpContext context, Exception exception, int statusCode, string message, string? correlationId)
    {
        if (!AuditErrorPolicy.ShouldAudit(statusCode))
            return;

        var entry = new SystemAuditEntry
        {
            Action = AuditErrorPolicy.ActionFor(statusCode),
            EventType = AuditErrorPolicy.EventTypeFor(statusCode),
            Status = AuditStatus.Failed,
            HttpStatusCode = statusCode,
            // Safe summary only — never the raw exception message (may hold secrets)
            // for 5xx; the technical detail + stack trace lives in the app log.
            Description = statusCode >= 500 ? $"{exception.GetType().Name}: unhandled server error" : message,
            CorrelationId = correlationId,
            Endpoint = context.Request.Path,
            HttpMethod = context.Request.Method,
            AdditionalData = new Dictionary<string, object?> { ["exceptionType"] = exception.GetType().FullName }
        };

        // Never let auditing cancel with the request or throw (audit is fail-safe).
        await auditService.LogAsync(entry, CancellationToken.None);
    }

    private static (int StatusCode, string Message, string? ErrorCode, IEnumerable<string>? Errors) MapException(Exception exception) =>
        exception switch
        {
            Backend.Shared.Exceptions.NotFoundException ex =>
                ((int)HttpStatusCode.NotFound, ex.Message, ex.ErrorCode, null),

            AppValidationException ex =>
                ((int)HttpStatusCode.BadRequest, ex.Message, ex.ErrorCode,
                    ex.Errors.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}"))),

            FluentValidationException ex =>
                ((int)HttpStatusCode.BadRequest, "One or more validation errors occurred.", "ValidationError",
                    ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")),

            Backend.Shared.Exceptions.UnauthorizedException ex =>
                ((int)HttpStatusCode.Unauthorized, ex.Message, ex.ErrorCode, null),

            Backend.Shared.Exceptions.ForbiddenException ex =>
                ((int)HttpStatusCode.Forbidden, ex.Message, ex.ErrorCode, null),

            Backend.Shared.Exceptions.BusinessException ex =>
                ((int)HttpStatusCode.BadRequest, ex.Message, ex.ErrorCode, null),

            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.", "InternalServerError", null)
        };
}
