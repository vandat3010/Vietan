namespace Backend.Application.Common;

/// <summary>
/// BE 3.1a §3/§26 — strips secrets from audit metadata before persistence.
/// Never let a password, token, Authorization header, secret or connection string
/// land in the audit table. Pure and unit-testable.
/// </summary>
public static class AuditSanitizer
{
    public const string Redacted = "***REDACTED***";

    /// <summary>Max stored length for free-text fields to bound row size.</summary>
    public const int MaxTextLength = 2000;

    // Substring match (case-insensitive) against the metadata key name.
    private static readonly string[] SensitiveKeyFragments =
    [
        "password", "passwd", "pwd",
        "token", "jwt", "refresh",
        "authorization", "auth",
        "secret", "apikey", "api_key",
        "connectionstring", "connection_string", "conn",
        "credential", "cookie", "sessionkey"
    ];

    /// <summary>True when a metadata key looks sensitive and must be redacted.</summary>
    public static bool IsSensitiveKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var lower = key.Trim().ToLowerInvariant();
        return SensitiveKeyFragments.Any(fragment => lower.Contains(fragment, StringComparison.Ordinal));
    }

    /// <summary>
    /// Returns a copy of <paramref name="data"/> with sensitive values replaced by
    /// <see cref="Redacted"/> and long text values truncated. Null-safe.
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Sanitize(IReadOnlyDictionary<string, object?>? data)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (data is null)
            return result;

        foreach (var (key, value) in data)
        {
            if (IsSensitiveKey(key))
            {
                result[key] = Redacted;
                continue;
            }

            result[key] = value is string s ? Truncate(s) : value;
        }

        return result;
    }

    /// <summary>Truncates free text (Description/UserAgent) to a bounded length.</summary>
    public static string? Truncate(string? text, int maxLength = MaxTextLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength];
    }
}
