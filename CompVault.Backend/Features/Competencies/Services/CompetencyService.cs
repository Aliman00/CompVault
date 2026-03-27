using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Competencies.Services;

/// <summary>
/// Implementerer administrasjon av kompetansebevis.
/// </summary>
public sealed class CompetencyService(
    ICompetencyRepository competencyRepository,
    ICompetencyTypeRepository competencyTypeRepository) : ICompetencyService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CompetencyDto>>> GetAllAsync(
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Competency> competencies = await competencyRepository.GetAllWithDetailsAsync(
            userId, status, competencyTypeId, cancellationToken);

        var dtos = competencies.Select(CompetencyMapper.ToDto).ToList();

        return Result<IReadOnlyList<CompetencyDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<CompetencyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Competency? competency = await competencyRepository.GetWithDetailsAsync(id, cancellationToken);

        if (competency is null)
            return Result<CompetencyDto>.Failure(
                AppError.NotFound($"Kompetansebevis med ID '{id}' ble ikke funnet."));

        return Result<CompetencyDto>.Success(CompetencyMapper.ToDto(competency));
    }

    /// <inheritdoc />
    public async Task<Result<CompetencyDto>> CreateAsync(CreateCompetencyRequest request, CancellationToken cancellationToken = default)
    {
        CompetencyType? type = await competencyTypeRepository.GetByIdAsync(request.CompetencyTypeId, cancellationToken);

        if (type is null)
            return Result<CompetencyDto>.Failure(
                AppError.NotFound($"Kompetansetype med ID '{request.CompetencyTypeId}' ble ikke funnet."));

        // Validering av ExpiryDate basert på typens RequiresExpiration
        if (type.RequiresExpiration && request.ExpiryDate is null)
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Kompetansetypen '{type.Name}' krever en utløpsdato (RequiresExpiration = true)."));

        var competency = new Competency
        {
            UserId = request.UserId,
            CompetencyTypeId = request.CompetencyTypeId,
            ExpiryDate = type.RequiresExpiration ? request.ExpiryDate : null,
            IssuedDate = request.IssuedDate,
            CertificateNumber = request.CertificateNumber,
            Notes = request.Notes,
            Status = CompetencyStatusCalculator.Calculate(type.RequiresExpiration ? request.ExpiryDate : null),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await competencyRepository.AddAsync(competency, cancellationToken);
        await competencyRepository.SaveChangesAsync(cancellationToken);

        // Hent med navigasjon for å returnere fullstendig DTO
        Competency? created = await competencyRepository.GetWithDetailsAsync(competency.Id, cancellationToken);
        return Result<CompetencyDto>.Success(CompetencyMapper.ToDto(created!));
    }

    /// <inheritdoc />
    public async Task<Result<CompetencyDto>> UpdateAsync(Guid id, UpdateCompetencyRequest request, CancellationToken cancellationToken = default)
    {
        Competency? competency = await competencyRepository.GetWithDetailsAsync(id, cancellationToken);

        if (competency is null)
            return Result<CompetencyDto>.Failure(
                AppError.NotFound($"Kompetansebevis med ID '{id}' ble ikke funnet."));

        // Håndter revoke
        if (request.Status == CompetencyStatus.Revoked)
        {
            if (string.IsNullOrWhiteSpace(request.RevokedReason))
                return Result<CompetencyDto>.Failure(
                    AppError.Create(ErrorCode.Validation,
                        "Årsak til tilbakekalling (RevokedReason) er påkrevd når status settes til Revoked."));

            competency.Status = CompetencyStatus.Revoked;
            competency.RevokedAt = DateTime.UtcNow;
            competency.RevokedReason = request.RevokedReason;
        }
        else if (request.Status.HasValue && competency.Status == CompetencyStatus.Revoked)
        {
            // Endres fra Revoked til noe annet — nullstill revocation-felt
            competency.RevokedAt = null;
            competency.RevokedReason = null;
            competency.Status = request.Status.Value;
        }

        if (request.ExpiryDate.HasValue)
            competency.ExpiryDate = request.ExpiryDate.Value;

        if (request.IssuedDate.HasValue)
            competency.IssuedDate = request.IssuedDate.Value;

        if (request.CertificateNumber is not null)
            competency.CertificateNumber = request.CertificateNumber;

        if (request.Notes is not null)
            competency.Notes = request.Notes;

        // Hvis expiry date ble endret og status ikke er revoked, kalkuler ny status
        if (request.ExpiryDate.HasValue && competency.Status != CompetencyStatus.Revoked)
            competency.Status = CompetencyStatusCalculator.Calculate(competency.ExpiryDate);

        await competencyRepository.UpdateAsync(competency, cancellationToken);
        await competencyRepository.SaveChangesAsync(cancellationToken);

        // Hent med navigasjon for å returnere fullstendig DTO
        Competency? updated = await competencyRepository.GetWithDetailsAsync(competency.Id, cancellationToken);
        return Result<CompetencyDto>.Success(CompetencyMapper.ToDto(updated!));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Competency? competency = await competencyRepository.GetByIdAsync(id, cancellationToken);

        if (competency is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Kompetansebevis med ID '{id}' ble ikke funnet."));

        await competencyRepository.SoftDeleteAsync(competency, cancellationToken);
        await competencyRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ExpiringCompetencyDto>>> GetExpiringAsync(
        Guid? userId,
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Competency> expiring = await competencyRepository.GetExpiringAsync(
            userId, departmentId, cancellationToken);

        var dtos = expiring.Select(CompetencyMapper.ToExpiringDto).ToList();

        return Result<IReadOnlyList<ExpiringCompetencyDto>>.Success(dtos);
    }
}
