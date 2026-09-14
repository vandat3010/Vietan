using Backend.Shared.Responses;
using Backend.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>
/// Map <see cref="Result{T}"/> → <see cref="ApiResponse{T}"/> (không throw).
/// NotFound → 404, Conflict → 409, Forbidden → 403, Validation → 400, Unexpected → 500.
/// </summary>
internal static class ScadaResultMapper
{
    public static IActionResult ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result,
        string successMessage)
    {
        if (result.IsSuccess)
            return controller.Ok(ApiResponse<T>.Ok(result.Value!, successMessage));

        var message = result.Errors.FirstOrDefault() ?? result.ErrorCode;
        var body = ApiResponse<T>.Fail(message, result.Errors);

        if (IsNotFound(result.ErrorCode))
            return controller.NotFound(body);

        if (IsConflict(result.ErrorCode))
            return controller.Conflict(body);

        if (IsForbidden(result.ErrorCode))
            return controller.StatusCode(StatusCodes.Status403Forbidden, body);

        if (result.ErrorCode is "ValidationError" ||
            result.ErrorCode.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorCode.Contains("Validation", StringComparison.OrdinalIgnoreCase) ||
            result.ErrorCode.Contains("PasswordPolicy", StringComparison.OrdinalIgnoreCase))
            return controller.BadRequest(body);

        if (result.ErrorCode.Contains("Unexpected", StringComparison.OrdinalIgnoreCase))
            return controller.StatusCode(StatusCodes.Status500InternalServerError, body);

        return controller.BadRequest(body);
    }

    public static IActionResult ToActionResult(
        this ControllerBase controller,
        Result result,
        string successMessage)
    {
        if (result.IsSuccess)
            return controller.Ok(ApiResponse.Ok(successMessage));

        var message = result.Errors.FirstOrDefault() ?? result.ErrorCode;
        var body = ApiResponse.Fail(message, result.Errors);

        if (IsNotFound(result.ErrorCode))
            return controller.NotFound(body);

        if (IsConflict(result.ErrorCode))
            return controller.Conflict(body);

        if (IsForbidden(result.ErrorCode))
            return controller.StatusCode(StatusCodes.Status403Forbidden, body);

        if (result.ErrorCode.Contains("Unexpected", StringComparison.OrdinalIgnoreCase))
            return controller.StatusCode(StatusCodes.Status500InternalServerError, body);

        return controller.BadRequest(body);
    }

    /// <summary>Success with empty body (DELETE/deactivate).</summary>
    public static IActionResult ToNoContentResult(
        this ControllerBase controller,
        Result result)
    {
        if (result.IsSuccess)
            return controller.NoContent();

        return controller.ToActionResult(result, string.Empty);
    }

    private static bool IsNotFound(string errorCode) =>
        errorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase);

    private static bool IsConflict(string errorCode) =>
        errorCode.Contains("Conflict", StringComparison.OrdinalIgnoreCase)
        || errorCode.Contains("UsernameTaken", StringComparison.OrdinalIgnoreCase)
        || errorCode.Contains("Duplicate", StringComparison.OrdinalIgnoreCase);

    private static bool IsForbidden(string errorCode) =>
        errorCode.Contains("Forbidden", StringComparison.OrdinalIgnoreCase)
        || errorCode.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
            && !errorCode.Contains("Auth.Unauthorized", StringComparison.Ordinal);
}
