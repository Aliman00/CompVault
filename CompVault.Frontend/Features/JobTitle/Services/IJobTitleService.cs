using CompVault.Shared.DTOs.JobTitles;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.JobTitle.Services;

public interface IJobTitleService
{
    /// <summary>
    /// Henter alle aktive stillinger fra backend
    /// </summary>
    Task<Result<List<JobTitleDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Henter en aktiv stilling fra backend
    /// </summary>
    Task<Result<JobTitleDto?>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter en ny stilling
    /// </summary>
    Task<Result<JobTitleDto>> CreateAsync(CreateJobTitleRequest request, CancellationToken ct);
    
    /// <summary>
    /// Oppdaterer eksisterende stilling
    /// </summary>
    Task<Result<JobTitleDto>> UpdateAsync(Guid id, UpdateJobTitleRequest request, CancellationToken ct);
    
    /// <summary>
    /// Soft-deleter en stilling
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}