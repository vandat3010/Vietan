using Backend.Application.DTOs.Audit;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

public interface IClientAuditService
{
    /// <summary>
    /// Persists a client-reported event via <see cref="ISystemAuditService"/>.
    /// Rejects backend-owned security actions. Actor/IP from current user.
    /// </summary>
    Task<Result<ClientAuditLogResponse>> CreateAsync(
        CreateClientAuditLogRequest request,
        CancellationToken cancellationToken = default);
}
