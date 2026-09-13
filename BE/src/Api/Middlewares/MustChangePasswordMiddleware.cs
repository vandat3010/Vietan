using System.Text.Json;
using Backend.Application.Common;
using Backend.Infrastructure.Persistence.Context;
using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Middlewares;

/// <summary>
/// BE 1.3a — blocks business APIs while an authenticated user has
/// <c>scada.users.must_change_password = true</c>. Runs AFTER authentication so
/// <c>HttpContext.User</c> is populated. Whitelisted actions (change-password,
/// logout, me) are marked with <see cref="AllowWhenPasswordChangeRequiredAttribute"/>;
/// anonymous endpoints (login/register/refresh/forgot/reset) pass through untouched.
/// </summary>
public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Only authenticated users can be blocked. When JWT auth is disabled
        // No authenticated principal → nothing to enforce (anonymous endpoints).
        // When JWT is enabled, unauthenticated callers never reach this for [Authorize] routes.
        var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();
        var userId = currentUser.OperatorUserId;

        if (userId is null || IsExempt(context.GetEndpoint()))
        {
            await next(context);
            return;
        }

        var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var mustChange = await db.ScadaUsers
            .AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => (bool?)u.MustChangePassword)
            .FirstOrDefaultAsync(context.RequestAborted);

        if (mustChange == true)
        {
            await WriteForbiddenAsync(context);
            return;
        }

        await next(context);
    }

    /// <summary>
    /// Exempt when the endpoint is anonymous or explicitly opted in via
    /// <see cref="AllowWhenPasswordChangeRequiredAttribute"/>. Metadata-based only —
    /// no path/substring matching, so bypass via crafted URLs is not possible.
    /// </summary>
    public static bool IsExempt(Endpoint? endpoint)
    {
        if (endpoint is null)
            return true; // unmatched/static — not a business API

        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            return true;

        return endpoint.Metadata.GetMetadata<AllowWhenPasswordChangeRequiredAttribute>() is not null;
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var id)
            ? id?.ToString()
            : context.TraceIdentifier;

        var body = new ErrorResponse
        {
            Message = "You must change your password before continuing.",
            ErrorCode = AuthErrorCodes.PasswordChangeRequired,
            TraceId = correlationId,
            StatusCode = StatusCodes.Status403Forbidden,
            Path = context.Request.Path
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
