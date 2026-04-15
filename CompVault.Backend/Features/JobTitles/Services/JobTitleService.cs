using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
using CompVault.Shared.DTOs.JobTitles;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.JobTitles.Services;

/// <summary>
/// Implementerer stillingstittel-administrasjon.
/// </summary>
public sealed class JobTitleService(
    IJobTitleRepository jobTitleRepository,
    ILogger<JobTitleService> logger) : IJobTitleService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<JobTitleDto>>> GetAllAsync(CancellationToken ct)
    {
        IReadOnlyList<JobTitle> jobTitles = await jobTitleRepository.GetAllAsync(ct);

        var dtos = jobTitles
            .Select(JobTitleMapper.ToDto)
            .ToList();

        return Result<IReadOnlyList<JobTitleDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<JobTitleDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        JobTitle? jobTitle = await jobTitleRepository.GetByIdAsync(id, ct);

        if (jobTitle is null)
            return Result<JobTitleDto>.Failure(
                AppError.NotFound($"Stillingstittel med ID '{id}' ble ikke funnet."));

        return Result<JobTitleDto>.Success(JobTitleMapper.ToDto(jobTitle));
    }

    /// <inheritdoc />
    public async Task<Result<JobTitleDto>> CreateAsync(CreateJobTitleRequest request, CancellationToken ct)
    {
        bool nameExists = await jobTitleRepository.NameExistsAsync(request.Name, ct);

        if (nameExists)
        {
            logger.LogWarning("Kunne ikke opprette stillingstittel: navn {Name} finnes allerede", request.Name);
            return Result<JobTitleDto>.Failure(
                AppError.Conflict($"En stillingstittel med navn '{request.Name}' eksisterer allerede."));
        }

        var jobTitle = new JobTitle
        {
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await jobTitleRepository.AddAsync(jobTitle, ct);

        try
        {
            await jobTitleRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Navneunikhet sjekkes før lagring, men concurrent requests kan likevel
            // opprette samme navn. Unique constraint fanger dette opp.
            return Result<JobTitleDto>.Failure(
                AppError.Conflict($"En stillingstittel med navn '{request.Name}' eksisterer allerede."));
        }

        logger.LogInformation("Stillingstittel {Name} opprettet", request.Name);
        return Result<JobTitleDto>.Success(JobTitleMapper.ToDto(jobTitle));
    }

    /// <inheritdoc />
    public async Task<Result<JobTitleDto>> UpdateAsync(Guid id, UpdateJobTitleRequest request, CancellationToken ct)
    {
        JobTitle? jobTitle = await jobTitleRepository.GetByIdAsync(id, ct);

        if (jobTitle is null)
            return Result<JobTitleDto>.Failure(
                AppError.NotFound($"Stillingstittel med ID '{id}' ble ikke funnet."));

        bool nameConflict = await jobTitleRepository.NameExistsAsync(request.Name, ct)
            && !string.Equals(jobTitle.Name, request.Name, StringComparison.OrdinalIgnoreCase);

        if (nameConflict)
        {
            logger.LogWarning("Kunne ikke oppdatere stillingstittel {Id}: navn {Name} finnes allerede", id, request.Name);
            return Result<JobTitleDto>.Failure(
                AppError.Conflict($"En stillingstittel med navn '{request.Name}' eksisterer allerede."));
        }

        jobTitle.Name = request.Name.Trim();

        await jobTitleRepository.UpdateAsync(jobTitle, ct);

        try
        {
            await jobTitleRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent navneendring kan trigge unique constraint
            return Result<JobTitleDto>.Failure(
                AppError.Conflict($"En stillingstittel med navn '{request.Name}' eksisterer allerede."));
        }

        logger.LogInformation("Stillingstittel {Id} oppdatert", id);
        return Result<JobTitleDto>.Success(JobTitleMapper.ToDto(jobTitle));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct)
    {
        JobTitle? jobTitle = await jobTitleRepository.GetByIdAsync(id, ct);

        if (jobTitle is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Stillingstittel med ID '{id}' ble ikke funnet."));

        await jobTitleRepository.SoftDeleteAsync(jobTitle, ct);
        await jobTitleRepository.SaveChangesAsync(ct);

        logger.LogInformation("Stillingstittel {Id} slettet (soft delete)", id);
        return Result<bool>.Success(true);
    }
}
