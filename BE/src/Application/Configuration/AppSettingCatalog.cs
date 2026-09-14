using Backend.Application.Common;

namespace Backend.Application.Configuration;

/// <summary>
/// Classification of configuration sources. Secrets must never go into AppSettings DB.
/// </summary>
public enum AppSettingSourceKind
{
    /// <summary>Runtime editable key/value in app.app_settings (or scada.app_settings).</summary>
    DatabaseAppSettings = 0,

    /// <summary>Bound from appsettings / ENV via IOptions (not editable via AppSettings API).</summary>
    Options = 1,

    /// <summary>Connection strings, JWT signing key — ENV / User Secrets only.</summary>
    SecretOrInfrastructure = 2,

    /// <summary>FE-only localStorage (not a BE source of truth).</summary>
    FrontendLocal = 3
}

public sealed record AppSettingDefinition(
    string Key,
    string DataType,
    string Description,
    AppSettingSourceKind Source,
    bool EditableViaApi,
    string? DefaultValue = null,
    double? Min = null,
    double? Max = null,
    IReadOnlyList<string>? AllowedValues = null);

/// <summary>
/// Allowlist + validation catalog for AppSettings and related configuration.
/// Arbitrary keys are rejected for mutation.
/// </summary>
public static class AppSettingCatalog
{
    public const string SessionIdleTimeoutMinutes = SessionIdleTimeoutPolicy.SettingKey;

    public static IReadOnlyList<AppSettingDefinition> All { get; } =
    [
        new(
            SessionIdleTimeoutMinutes,
            "integer",
            "Idle timeout (minutes) for non-admin sessions.",
            AppSettingSourceKind.DatabaseAppSettings,
            EditableViaApi: true,
            DefaultValue: SessionIdleTimeoutPolicy.DefaultMinutes.ToString(),
            Min: SessionIdleTimeoutPolicy.MinMinutes,
            Max: SessionIdleTimeoutPolicy.MaxMinutes),

        new(
            "password.expireDays",
            "integer",
            "Password lifetime in days (IOptions:Password:ExpireDays).",
            AppSettingSourceKind.Options,
            EditableViaApi: false,
            DefaultValue: "90",
            Min: 1),

        new(
            "password.warnBeforeDays",
            "integer",
            "Days before expiry to warn (IOptions:Password:WarnBeforeDays).",
            AppSettingSourceKind.Options,
            EditableViaApi: false,
            DefaultValue: "14",
            Min: 0),

        new(
            "login.maxFailed",
            "integer",
            "Failed login threshold (IOptions:Login:MaxFailed).",
            AppSettingSourceKind.Options,
            EditableViaApi: false),

        new(
            "login.windowMinutes",
            "integer",
            "Failed login window (IOptions:Login:WindowMinutes).",
            AppSettingSourceKind.Options,
            EditableViaApi: false),

        new(
            "login.lockMinutes",
            "integer",
            "Account lock duration (IOptions:Login:LockMinutes).",
            AppSettingSourceKind.Options,
            EditableViaApi: false),

        new(
            "jwt.signingKey",
            "secret",
            "JWT HS signing key — ENV / User Secrets only.",
            AppSettingSourceKind.SecretOrInfrastructure,
            EditableViaApi: false),

        new(
            "connectionStrings.default",
            "secret",
            "PostgreSQL connection string — ENV / User Secrets.",
            AppSettingSourceKind.SecretOrInfrastructure,
            EditableViaApi: false),

        new(
            "connectionStrings.redis",
            "secret",
            "Redis connection string — ENV / User Secrets.",
            AppSettingSourceKind.SecretOrInfrastructure,
            EditableViaApi: false),

        new(
            "fe.passwordPolicy",
            "object",
            "FE localStorage password policy — not BE source of truth.",
            AppSettingSourceKind.FrontendLocal,
            EditableViaApi: false)
    ];

    public static bool TryGet(string key, out AppSettingDefinition definition)
    {
        definition = All.FirstOrDefault(d =>
            string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase))!;
        return definition is not null;
    }

    public static bool TryValidateEditableValue(string key, string? value, out string error)
    {
        if (!TryGet(key, out var def) || !def.EditableViaApi)
        {
            error = $"Setting key '{key}' is unknown or not editable via API.";
            return false;
        }

        if (string.Equals(def.DataType, "integer", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(value, out var n))
            {
                error = $"Setting '{key}' requires an integer value.";
                return false;
            }

            if (def.Min is { } min && n < min)
            {
                error = $"Setting '{key}' must be >= {min}.";
                return false;
            }

            if (def.Max is { } max && n > max)
            {
                error = $"Setting '{key}' must be <= {max}.";
                return false;
            }
        }

        if (def.AllowedValues is { Count: > 0 } allowed
            && !allowed.Contains(value ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            error = $"Setting '{key}' value is not in the allowed set.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
