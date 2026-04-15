using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Competencies.Services;

public interface ICompetencyService
{
    /// <summary>
    /// Henter alle kompetansebevis fra backend TODO: Med query parameter. Ikke implementert
    /// </summary>
    Task<Result<List<CompetencyDto>>> GetAllAsync(CancellationToken ct);
    
    /// <summary>
    /// Henter et kompetansebevis fra backend
    /// </summary>
    Task<Result<CompetencyDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter et ny kompetansebevis
    /// </summary>
    Task<Result<CompetencyDto>> CreateAsync(CreateCompetencyRequest request, CancellationToken ct);
    
    /// <summary>
    /// Oppdaterer eksisterende kompetansebevis
    /// </summary>
    Task<Result<CompetencyDto>> UpdateAsync(Guid id, UpdateCompetencyRequest request, CancellationToken ct);
    
    /// <summary>
    /// Sletter et kompetansebevis
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Henter alle utgåtte kompetansebevis TODO: Med query parameter. Ikke implementert
    /// </summary>
    Task<Result<List<ExpiringCompetencyDto>>> GetExpiringAsync(CancellationToken ct);
}