namespace Backend.Shared.Responses;

/// <summary>
/// Standard envelope for every API response (success or failure) so that
/// consumers of the API always deal with one predictable JSON shape.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public string? TraceId { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Request completed successfully", string? traceId = null) =>
        new() { Success = true, Message = message, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(string message, IEnumerable<string>? errors = null, string? traceId = null) =>
        new() { Success = false, Message = message, Errors = errors, TraceId = traceId };
}

/// <summary>
/// Non-generic variant for endpoints that don't return a payload (e.g. Delete, Logout).
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public IEnumerable<string>? Errors { get; init; }
    public string? TraceId { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public static ApiResponse Ok(string message = "Request completed successfully", string? traceId = null) =>
        new() { Success = true, Message = message, TraceId = traceId };

    public static ApiResponse Fail(string message, IEnumerable<string>? errors = null, string? traceId = null) =>
        new() { Success = false, Message = message, Errors = errors, TraceId = traceId };
}
