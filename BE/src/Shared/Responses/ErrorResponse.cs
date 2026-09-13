namespace Backend.Shared.Responses;

/// <summary>
/// Detailed error payload returned by the global exception middleware.
/// Kept separate from <see cref="ApiResponse{T}"/> so error shapes can evolve
/// (e.g. adding RFC-7807 fields) without touching every success response.
/// </summary>
public class ErrorResponse
{
    public bool Success { get; init; } = false;
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public IEnumerable<string>? Errors { get; init; }
    public string? TraceId { get; init; }
    public int StatusCode { get; init; }
    public string? Path { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
#if DEBUG
    public string? StackTrace { get; init; }
#endif
}
