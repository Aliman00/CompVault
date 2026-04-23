using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Audit.Services;

/// <summary>
/// Service for å hente revisjonslogg med filtrering og paginering.
/// </summary>
public interface IAuditLogService
{
    /// <summary>Henter revisjonslogg med filtrering og paginering.</summary>
    Task<Result<PagedResult<AuditLogDto>>> GetAsync(
        AuditLogQueryParameters parameters, CancellationToken cancellationToken = default);
}