using CompVault.Shared.Enums;
namespace CompVault.Frontend.Features.Competencies.Models;

/// <summary>
/// Record for å bygge en URL med query-parametere for kompetansebevis
/// </summary>
/// <param name="UserId">Valgfritt filtering på en bruker</param>
/// <param name="Status">Valgfritt filtering på status til kompetansebevis</param>
/// <param name="CompetencyTypeId">Valgfritt filtering på kompetansetype</param>
public sealed record CompetencyFilterRequest(
    Guid? UserId = null,
    CompetencyStatus? Status = null,
    Guid? CompetencyTypeId = null);