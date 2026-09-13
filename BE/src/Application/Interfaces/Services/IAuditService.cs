using Backend.Application.DTOs.Audit;
using Backend.Shared.Pagination;
using Backend.Shared.Results;

namespace Backend.Application.Interfaces.Services;

/// <summary>
/// Writes and reads the audit trail.
/// <para>
/// This complements - it does not replace - the automatic
/// CreatedBy/CreatedDate/ModifiedBy/ModifiedDate stamping that
/// ApplicationDbContext performs on every entity. Those columns answer "who last
/// touched this row"; an audit log answers "what exactly changed, and what did it
/// look like before", plus non-entity events such as logins and exports that have
/// no row to stamp.
/// </para>
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Appends an entry. The current user, IP and correlation id are filled in by
    /// the implementation from the ambient request context, so callers only supply
    /// the "what".
    /// </summary>
    Task CreateAuditLogAsync(CreateAuditLogDto entry, CancellationToken cancellationToken = default);

    Task<Result<PaginationResult<AuditLogDto>>> GetAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
