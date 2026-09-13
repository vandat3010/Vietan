using Backend.Application.DTOs.Industrial;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Industrial;

/// <summary>
/// Forward-looking contract for the SCADA/industrial module: no implementation is
/// registered yet, so resolving this from DI will fail until one is added. It ships
/// with the template so device-facing endpoints and background workers can be
/// written against a stable shape, and so the eventual implementation in
/// <c>src\Infrastructure\Industrial\</c> is a DI registration away rather than an
/// Application-layer rewrite.
/// </summary>
public interface IDeviceService
{
    Task<Result<DeviceStatusDto>> GetDeviceStatusAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>Explicit state transition driven by an operator or a diagnostic, kept separate from <see cref="HeartbeatAsync"/> so liveness traffic never overwrites a deliberate state such as Maintenance.</summary>
    Task<Result> UpdateDeviceStatusAsync(Guid deviceId, DeviceState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liveness ping. The timestamp is a parameter rather than the server clock
    /// because gateways buffer heartbeats across network outages and the original
    /// time is what makes the gap diagnosable.
    /// </summary>
    Task<Result> HeartbeatAsync(Guid deviceId, DateTime timestampUtc, CancellationToken cancellationToken = default);
}
