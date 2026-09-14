namespace Backend.Application.DTOs.Licenses;

public class SystemLicenseDto
{
    public Guid Id { get; set; }
    /// <summary>Masked license key (never full secret).</summary>
    public string LicenseKeyMasked { get; set; } = string.Empty;
    public int MaxConcurrentUsers { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class CreateSystemLicenseRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public int MaxConcurrentUsers { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

public class UpdateSystemLicenseRequest
{
    /// <summary>Optional rotate — omit to keep existing key.</summary>
    public string? LicenseKey { get; set; }
    public int? MaxConcurrentUsers { get; set; }
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool? IsEnabled { get; set; }
    public string? Description { get; set; }
}
