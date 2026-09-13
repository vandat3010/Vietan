using System.Globalization;

namespace Backend.Application.Common;

/// <summary>
/// BE 2.1 — canonical parsing/validation for the <c>session.idleTimeoutMinutes</c>
/// app-setting. Idle detection itself is a FRONTEND concern (§12, §14): the
/// backend never tracks activity server-side. This helper only defines the shared
/// contract for interpreting the stored string value so an invalid/missing config
/// resolves to an explicit, safe default rather than silently becoming 0
/// (instant logout) or infinite (no logout).
/// </summary>
public static class SessionIdleTimeoutPolicy
{
    /// <summary>The app-setting key served to the frontend.</summary>
    public const string SettingKey = "session.idleTimeoutMinutes";

    /// <summary>Safe fallback (minutes) when the stored value is missing or invalid.</summary>
    public const int DefaultMinutes = 10;

    /// <summary>Minimum allowed idle timeout (minutes).</summary>
    public const int MinMinutes = 1;

    /// <summary>Maximum allowed idle timeout (minutes) — 7 days.</summary>
    public const int MaxMinutes = 10080;

    /// <summary>
    /// Parses the raw app-setting value. Valid only when it is an integer &gt; 0.
    /// On any invalid/missing input, <paramref name="minutes"/> is set to
    /// <see cref="DefaultMinutes"/> and the method returns <c>false</c> so callers
    /// can surface a warning instead of accepting a dangerous 0/infinite timeout.
    /// </summary>
    public static bool TryParseMinutes(string? rawValue, out int minutes)
    {
        if (!string.IsNullOrWhiteSpace(rawValue)
            && int.TryParse(rawValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed >= MinMinutes
            && parsed <= MaxMinutes)
        {
            minutes = parsed;
            return true;
        }

        minutes = DefaultMinutes;
        return false;
    }

    /// <summary>Parsed minutes when valid, otherwise <see cref="DefaultMinutes"/>.</summary>
    public static int ResolveMinutes(string? rawValue) =>
        TryParseMinutes(rawValue, out var minutes) ? minutes : minutes;
}
