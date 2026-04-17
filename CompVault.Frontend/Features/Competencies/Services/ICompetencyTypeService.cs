using CompVault.Shared.DTOs.CompetencyTypes;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Competencies.Services;

public interface ICompetencyTypeService
{
    /// <summary>
    /// Henter alle kompetansebevistyper fra backend TODO: Med query parameter. Ikke implementert
    /// </summary>
    Task<Result<List<CompetencyTypeDto>>> GetAllAsync(CancellationToken ct);
    
    /// <summary>
    /// Henter et kompetansebevistyper fra backend
    /// </summary>
    Task<Result<CompetencyTypeDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter et ny kompetansebevistyper
    /// </summary>
    Task<Result<CompetencyTypeDto>> CreateAsync(CreateCompetencyTypeRequest request, CancellationToken ct);
    
    /// <summary>
    /// Oppdaterer eksisterende kompetansebevistyper
    /// </summary>
    Task<Result<CompetencyTypeDto>> UpdateAsync(Guid id, UpdateCompetencyTypeRequest request, CancellationToken ct);
    
    /// <summary>
    /// Sletter et kompetansebevistyper
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}