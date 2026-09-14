using Backend.Application.DTOs.Licenses;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

public interface ISystemLicenseAdminService
{
    Task<Result<IReadOnlyList<SystemLicenseDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<SystemLicenseDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<SystemLicenseDto>> CreateAsync(CreateSystemLicenseRequest request, CancellationToken cancellationToken = default);
    Task<Result<SystemLicenseDto>> UpdateAsync(Guid id, UpdateSystemLicenseRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
