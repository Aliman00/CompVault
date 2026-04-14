using CompVault.Shared.DTOs.JobTitles;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.JobTitles.Services;

/// <summary>
/// Stillingstittel-administrasjon — henting, oppretting, oppdatering og sletting.
/// </summary>
public interface IJobTitleService
{
    /// <summary>Henter alle aktive stillingstitler.</summary>
    Task<Result<IReadOnlyList<JobTitleDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>Henter én stillingstittel basert på ID.</summary>
    Task<Result<JobTitleDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Oppretter en ny stillingstittel.</summary>
    Task<Result<JobTitleDto>> CreateAsync(CreateJobTitleRequest request, CancellationToken ct);

    /// <summary>Oppdaterer en eksisterende stillingstittel.</summary>
    Task<Result<JobTitleDto>> UpdateAsync(Guid id, UpdateJobTitleRequest request, CancellationToken ct);

    /// <summary>Soft-sletter en stillingstittel.</summary>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct);
}