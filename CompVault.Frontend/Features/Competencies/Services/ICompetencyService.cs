using CompVault.Frontend.Features.Competencies.Models;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Competencies.Services;

public interface ICompetencyService
{
    /// <summary>
    /// Henter alle kompetansebevis fra backend med valgtfrie query-parametere
    /// </summary>
    Task<Result<PagedResult<CompetencyDto>>> GetAllAsync(CompetencyFilterRequest? filter, CancellationToken ct);

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
}