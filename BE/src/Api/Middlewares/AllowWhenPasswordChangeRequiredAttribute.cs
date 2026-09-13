namespace Backend.Api.Middlewares;

/// <summary>
/// Marks an action/controller as allowed even when the authenticated user has
/// <c>MustChangePassword = true</c> (BE 1.3a). Applied to change-password, logout, me.
/// The middleware whitelists by this metadata — never by URL substring — so a
/// business endpoint cannot be bypassed just because its path contains "password".
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AllowWhenPasswordChangeRequiredAttribute : Attribute;
