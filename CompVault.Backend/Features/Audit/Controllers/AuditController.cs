using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Audit.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Audit.Controllers;

/// <summary>
/// API for å hente revisjonslogg. Kun tilgjengelig for brukere med audit:read.
/// </summary>
[Authorize(Policy = Permissions.AuditRead)]
public sealed class AuditController(IAuditLogService auditLogService) : BaseController
{
    /// <summary>
    /// Henter revisjonslogg med filtrering og paginering.
    /// </summary>
    [HttpGet(ApiRoutes.Audit.Base)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get(
        [FromQuery] AuditLogQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        Result<PagedResult<AuditLogDto>> result = await auditLogService.GetAsync(parameters, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
}
