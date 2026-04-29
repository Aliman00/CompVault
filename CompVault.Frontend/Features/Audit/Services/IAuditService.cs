using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Audit;

public interface IAuditService
{
    /// <summary>
    /// Henter revisjonslogg med filtrering og paginering
    /// </summary>
    Task<Result<PagedResult<AuditLogDto>>> GetAsync(AuditLogQueryParameters parameters, CancellationToken ct);
}