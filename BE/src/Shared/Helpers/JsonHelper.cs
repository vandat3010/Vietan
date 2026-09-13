using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Shared.Helpers;

/// <summary>
/// Thin wrapper around <see cref="JsonSerializer"/> with one project-wide default
/// (camelCase, case-insensitive, enums as strings). Use this instead of calling
/// <see cref="JsonSerializer"/> directly so every layer serializes consistently
/// (e.g. audit-log payloads, cache entries, outbox messages).
/// </summary>
public static class JsonHelper
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize<T>(T value, JsonSerializerOptions? options = null) =>
        JsonSerializer.Serialize(value, options ?? DefaultOptions);

    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, options ?? DefaultOptions);

    /// <summary>Deserializes, returning <c>false</c> instead of throwing on malformed JSON.</summary>
    public static bool TryDeserialize<T>(string json, out T? value, JsonSerializerOptions? options = null)
    {
        try
        {
            value = Deserialize<T>(json, options);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}
