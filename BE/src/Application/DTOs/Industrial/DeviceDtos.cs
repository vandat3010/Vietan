namespace Backend.Application.DTOs.Industrial;

/// <summary>
/// Snapshot of a field device as the API exposes it. Kept as a DTO (not a Domain
/// entity) because the industrial modules are not modelled yet - see
/// <see cref="Backend.Application.Interfaces.Industrial.IDeviceService"/>.
/// </summary>
public class DeviceStatusDto
{
    public Guid DeviceId { get; set; }
    public string Name { get; set; } = default!;

    /// <summary>Stable human-readable identifier used by SCADA/HMI screens and operators, unlike the surrogate <see cref="DeviceId"/>.</summary>
    public string Code { get; set; } = default!;

    public DeviceState State { get; set; }

    /// <summary>Null until the device has reported at least once, which is how a never-seen device is told apart from a stale one.</summary>
    public DateTime? LastHeartbeatUtc { get; set; }

    /// <summary>
    /// Exposed separately from <see cref="State"/> because liveness is derived from
    /// heartbeat age against a configured timeout, while <see cref="State"/> is the
    /// last state the device (or an operator) explicitly reported.
    /// </summary>
    public bool IsOnline { get; set; }
}

/// <summary>
/// <see cref="Unknown"/> is the default so a device that has never reported is
/// never mistaken for one that deliberately reported <see cref="Offline"/>.
/// </summary>
public enum DeviceState
{
    Unknown = 0,
    Online = 1,
    Offline = 2,
    Faulted = 3,
    Maintenance = 4
}
