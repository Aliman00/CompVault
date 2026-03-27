using CompVault.Shared.DTOs.CompetencyTypes;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Competencies.Services;

/// <summary>
/// Administrasjon av kompetansetyper — henting, oppretting, oppdatering og sletting.
/// </summary>
public interface ICompetencyTypeService
{
    /// <summary>Henter alle aktive kompetansetyper.</summary>
    Task<Result<IReadOnlyList<CompetencyTypeDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter én kompetansetype basert på ID.</summary>
    Task<Result<CompetencyTypeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Oppretter en ny kompetansetype.</summary>
    Task<Result<CompetencyTypeDto>> CreateAsync(CreateCompetencyTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer en eksisterende kompetansetype.</summary>
    Task<Result<CompetencyTypeDto>> UpdateAsync(Guid id, UpdateCompetencyTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter en kompetansetype.</summary>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
