namespace Backend.Shared.Constants;

/// <summary>
/// HTTP status dùng trong API SCADA (Stations và metadata).
/// Bọc <c>Microsoft.AspNetCore.Http.StatusCodes</c> để tài liệu / controller thống nhất.
/// </summary>
public static class ScadaHttpStatuses
{
    public const int Ok = 200;
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int NotFound = 404;
    public const int InternalServerError = 500;
}
