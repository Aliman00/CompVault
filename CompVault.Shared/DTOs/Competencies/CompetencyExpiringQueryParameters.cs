using CompVault.Shared.DTOs.Common.Pagination;

namespace CompVault.Shared.DTOs.Competencies;

/// <summary>
/// Query-parametere for filtrering og paginering av utløpende kompetansebevis.
/// Arver paginering fra <see cref="PagedQuery"/> og legger til filtre for utløpende bevis.
/// </summary>
public record class CompetencyExpiringQueryParameters : PagedQuery
{
    public Guid? UserId { get; set; }
    public Guid? DepartmentId { get; set; }
}