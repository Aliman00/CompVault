using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Competencies.Services;

/// <summary>
/// Implementerer administrasjon av kompetansebevis.
/// </summary>
public sealed class CompetencyService(
    ICompetencyRepository competencyRepository,
    ICompetencyTypeRepository competencyTypeRepository,
    IUserRepository userRepository) : ICompetencyService
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
        // [Required] validering i DTO garanterer at disse verdiene er satt
        Guid userId = request.UserId!.Value;
        Guid competencyTypeId = request.CompetencyTypeId!.Value;
        DateTime issuedDate = request.IssuedDate!.Value;

        CompetencyType? type = await competencyTypeRepository.GetByIdAsync(competencyTypeId, cancellationToken);

        if (type is null)
            return Result<CompetencyDto>.Failure(
                AppError.NotFound($"Kompetansetype med ID '{competencyTypeId}' ble ikke funnet."));

        if (!type.IsActive)
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Kompetansetypen '{type.Name}' er inaktiv og kan ikke brukes."));

        bool userExists = await userRepository.ExistsAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
            return Result<CompetencyDto>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));

        // Validering av ExpiryDate basert på typens RequiresExpiration
        if (type.RequiresExpiration && request.ExpiryDate is null)
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Kompetansetypen '{type.Name}' krever en utløpsdato (RequiresExpiration = true)."));

        // Validering av ExpiryDate >= IssuedDate
        if (request.ExpiryDate.HasValue && request.ExpiryDate.Value < issuedDate)
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Utløpsdato (ExpiryDate) kan ikke være før utstedelsesdato (IssuedDate)."));

        var competency = new Competency
        {
            UserId = userId,
            CompetencyTypeId = competencyTypeId,
            ExpiryDate = type.RequiresExpiration ? request.ExpiryDate : null,
            IssuedDate = issuedDate,
            CertificateNumber = request.CertificateNumber,
            Notes = request.Notes,
            Status = CompetencyStatusCalculator.Calculate(type.RequiresExpiration ? request.ExpiryDate : null),
            IsActive = true
        };

        await competencyRepository.AddAsync(competency, cancellationToken);
        await competencyRepository.SaveChangesAsync(cancellationToken);

        // Hent med navigasjon for å returnere fullstendig DTO
        Competency created = await competencyRepository.GetWithDetailsAsync(competency.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Kompetansebevis med ID '{competency.Id}' ble ikke funnet etter opprettelse.");

        return Result<CompetencyDto>.Success(CompetencyMapper.ToDto(created));
    }

    /// <inheritdoc />
    public async Task<Result<CompetencyDto>> UpdateAsync(Guid id, UpdateCompetencyRequest request, CancellationToken cancellationToken = default)
    {
        // Tracking query med navigasjon — unngår GetByIdAsync + UpdateAsync + ekstra GetWithDetailsAsync
        Competency? competency = await competencyRepository.GetForUpdateAsync(id, cancellationToken);

        if (competency is null)
            return Result<CompetencyDto>.Failure(
                AppError.NotFound($"Kompetansebevis med ID '{id}' ble ikke funnet."));

        // Validering før mutasjon — CompetencyType er lastet via GetForUpdateAsync
        DateTime? effectiveExpiry = request.ExpiryDate ?? competency.ExpiryDate;
        DateTime effectiveIssued = request.IssuedDate ?? competency.IssuedDate;

        if (effectiveExpiry.HasValue && effectiveExpiry < effectiveIssued)
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Utløpsdato (ExpiryDate) kan ikke være før utstedelsesdato (IssuedDate)."));

        // Kun Revoked kan settes manuelt; andre statuser beregnes automatisk
        if (request.Status.HasValue && request.Status.Value != CompetencyStatus.Revoked)
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Status kan kun settes til '{CompetencyStatus.Revoked}'. Andre statusverdier beregnes automatisk."));

        bool isRevoked = competency.Status == CompetencyStatus.Revoked;
        bool revoking = request.Status == CompetencyStatus.Revoked;
        bool expiryChanged = request.ExpiryDate.HasValue;

        if (revoking)
        {
            if (string.IsNullOrWhiteSpace(request.RevokedReason))
                return Result<CompetencyDto>.Failure(
                    AppError.Create(ErrorCode.Validation,
                        "Årsak til tilbakekalling (RevokedReason) er påkrevd når status settes til Revoked."));

            competency.Status = CompetencyStatus.Revoked;
            competency.RevokedAt = DateTime.UtcNow;
            competency.RevokedReason = request.RevokedReason;
        }

        if (request.ExpiryDate.HasValue && competency.CompetencyType?.RequiresExpiration == false)
            competency.ExpiryDate = null;
        else if (request.ExpiryDate.HasValue)
            competency.ExpiryDate = request.ExpiryDate.Value;

        if (request.IssuedDate.HasValue)
            competency.IssuedDate = request.IssuedDate.Value;

        if (request.CertificateNumber is not null)
            competency.CertificateNumber = request.CertificateNumber;

        if (request.Notes is not null)
            competency.Notes = request.Notes;

        // Statuskalkulasjon: ved ExpiryDate-endring, men aldri hvis status er Revoked
        if (!revoking && expiryChanged && !isRevoked)
            competency.Status = CompetencyStatusCalculator.Calculate(competency.ExpiryDate);

        // Entity er allerede tracked via GetForUpdateAsync — ingen UpdateAsync nødvendig
        await competencyRepository.SaveChangesAsync(cancellationToken);

        // Navigasjon er allerede lastet — ingen ekstra query nødvendig
        return Result<CompetencyDto>.Success(CompetencyMapper.ToDto(competency));
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