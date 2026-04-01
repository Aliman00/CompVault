using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Shared.DTOs.CompetencyTypes;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Competencies.Services;

/// <summary>
/// Implementerer administrasjon av kompetansetyper.
/// </summary>
public sealed class CompetencyTypeService(
    ICompetencyTypeRepository competencyTypeRepository) : ICompetencyTypeService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CompetencyTypeDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CompetencyType> types = await competencyTypeRepository.GetAllAsync(cancellationToken);

        var dtos = types.Select(CompetencyMapper.ToTypeDto).ToList();

        return Result<IReadOnlyList<CompetencyTypeDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<CompetencyTypeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        CompetencyType? type = await competencyTypeRepository.GetByIdAsync(id, cancellationToken);

        if (type is null)
            return Result<CompetencyTypeDto>.Failure(
                AppError.NotFound($"Kompetansetype med ID '{id}' ble ikke funnet."));

        return Result<CompetencyTypeDto>.Success(CompetencyMapper.ToTypeDto(type));
    }

    /// <inheritdoc />
    public async Task<Result<CompetencyTypeDto>> CreateAsync(CreateCompetencyTypeRequest request, CancellationToken cancellationToken = default)
    {
        CompetencyType? existing = await competencyTypeRepository.GetByNameAsync(request.Name, cancellationToken);

        if (existing is not null)
            return Result<CompetencyTypeDto>.Failure(
                AppError.Create(ErrorCode.Validation, $"Kompetansetype med navn '{request.Name}' finnes allerede."));

        var type = new CompetencyType
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            RequiresExpiration = request.RequiresExpiration,
            IsActive = true
        };

        await competencyTypeRepository.AddAsync(type, cancellationToken);
        await competencyTypeRepository.SaveChangesAsync(cancellationToken);

        return Result<CompetencyTypeDto>.Success(CompetencyMapper.ToTypeDto(type));
    }

    /// <inheritdoc />
    public async Task<Result<CompetencyTypeDto>> UpdateAsync(Guid id, UpdateCompetencyTypeRequest request, CancellationToken cancellationToken = default)
    {
        CompetencyType? type = await competencyTypeRepository.GetByIdAsync(id, cancellationToken);

        if (type is null)
            return Result<CompetencyTypeDto>.Failure(
                AppError.NotFound($"Kompetansetype med ID '{id}' ble ikke funnet."));

        if (request.Name is not null)
        {
            if (request.Name != type.Name)
            {
                CompetencyType? existing = await competencyTypeRepository.GetByNameAsync(request.Name, cancellationToken);

                if (existing is not null)
                    return Result<CompetencyTypeDto>.Failure(
                        AppError.Create(ErrorCode.Validation, $"Kompetansetype med navn '{request.Name}' finnes allerede."));
            }

            type.Name = request.Name;
        }

        if (request.Description is not null)
            type.Description = request.Description;

        if (request.Category is not null)
            type.Category = request.Category;

        if (request.RequiresExpiration.HasValue && request.RequiresExpiration.Value != type.RequiresExpiration)
        {
            bool hasActiveCompetencies = await competencyTypeRepository.HasCompetenciesAsync(id, cancellationToken);

            if (hasActiveCompetencies)
                return Result<CompetencyTypeDto>.Failure(
                    AppError.Conflict("Kan ikke endre RequiresExpiration på en kompetansetype som har aktive kompetansebevis."));

            type.RequiresExpiration = request.RequiresExpiration.Value;
        }

        if (request.IsActive.HasValue)
            type.IsActive = request.IsActive.Value;

        await competencyTypeRepository.SaveChangesAsync(cancellationToken);

        return Result<CompetencyTypeDto>.Success(CompetencyMapper.ToTypeDto(type));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        CompetencyType? type = await competencyTypeRepository.GetByIdAsync(id, cancellationToken);

        if (type is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Kompetansetype med ID '{id}' ble ikke funnet."));

        bool hasActiveCompetencies = await competencyTypeRepository.HasCompetenciesAsync(id, cancellationToken);

        if (hasActiveCompetencies)
            return Result<bool>.Failure(
                AppError.Conflict("Kan ikke slette en kompetansetype som har aktive kompetansebevis."));

        await competencyTypeRepository.SoftDeleteAsync(type, cancellationToken);
        await competencyTypeRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}