using Backend.Shared.Constants;
using Backend.Shared.Responses;
using Backend.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers.Scada;

/// <summary>
/// Map <see cref="Result{T}"/> → <see cref="ApiResponse{T}"/> (không throw).
/// NotFound → 404, Validation → 400, Unexpected → 500, còn lại → 400.
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

        if (result.ErrorCode is "ValidationError" ||
            result.ErrorCode.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
            return controller.BadRequest(body);

        if (result.ErrorCode == ScadaErrorCodes.Unexpected)
            return controller.StatusCode(ScadaHttpStatuses.InternalServerError, body);

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

        if (result.ErrorCode == ScadaErrorCodes.Unexpected)
            return controller.StatusCode(ScadaHttpStatuses.InternalServerError, body);

        return controller.BadRequest(body);
    }

    private static bool IsNotFound(string errorCode) =>
        errorCode.Contains("NotFound", StringComparison.OrdinalIgnoreCase);
}
