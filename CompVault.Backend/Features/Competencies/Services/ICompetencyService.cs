using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Competencies.Services;

/// <summary>
/// Administrasjon av kompetansebevis — henting, oppretting, oppdatering, sletting
/// og henting av utløpende/utløpte bevis.
/// </summary>
public interface ICompetencyService
{
    /// <summary>
    /// Henter paginerte kompetansebevis med navigasjon, filtrert på valgfrie parametere.
    /// </summary>
    Task<Result<PagedResult<CompetencyDto>>> GetAllAsync(
        CompetencyQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>Henter ett kompetansebevis basert på ID med navigasjon.</summary>
    Task<Result<CompetencyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Oppretter et nytt kompetansebevis.</summary>
    Task<Result<CompetencyDto>> CreateAsync(CreateCompetencyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer et eksisterende kompetansebevis (inkl. revoke).</summary>
    Task<Result<CompetencyDto>> UpdateAsync(Guid id, UpdateCompetencyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter et kompetansebevis.</summary>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}