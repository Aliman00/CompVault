using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Enums;
namespace CompVault.Frontend.Features.Competencies.Models;

/// <summary>
/// Query-parametere for filtrering og paginering av kompetansebevis.
/// </summary>
public sealed record CompetencyFilterRequest : PagedQuery
{
    public Guid? UserId { get; init; }
    public CompetencyStatus? Status { get; init; }
    public Guid? CompetencyTypeId { get; init; }
}