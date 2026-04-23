using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Competencies;

/// <summary>
/// Query-parametere for filtrering og paginering av kompetansebevis.
/// Arver paginering fra <see cref="PagedQuery"/> og legger til kompetanse-spesifikke filtre.
/// </summary>
public record class CompetencyQueryParameters : PagedQuery
{
    public Guid? UserId { get; set; }
    public CompetencyStatus? Status { get; set; }
    public Guid? CompetencyTypeId { get; set; }
}