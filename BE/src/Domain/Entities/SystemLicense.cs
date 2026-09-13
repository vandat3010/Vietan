using Backend.Domain.Common;

namespace Backend.Domain.Entities;

/// <summary>
/// System concurrent-user license (schema <c>app</c>).
/// Overrides default MaxConcurrentUsers when enabled and within validity.
/// </summary>
public class SystemLicense : AuditableEntity
{
    /// <summary>Opaque license key (store as provided; never log full value).</summary>
    public string LicenseKey { get; set; } = string.Empty;

    public int MaxConcurrentUsers { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Description { get; set; }
}
