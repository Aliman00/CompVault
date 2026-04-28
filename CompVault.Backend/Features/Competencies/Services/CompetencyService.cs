using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Audit.Services;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
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
    IUserRepository userRepository,
    IAuditContext auditContext,
    IDepartmentScopeService departmentScope) : ICompetencyService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<CompetencyDto>>> GetAllAsync(
        CompetencyQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        int totalCount = await competencyRepository.CountWithFiltersAsync(
            queryParameters.UserId, queryParameters.Status, queryParameters.CompetencyTypeId,
            cancellationToken);

        IReadOnlyList<Competency> competencies = await competencyRepository.GetAllWithDetailsPagedAsync(
            queryParameters.Skip, queryParameters.PageSize,
            queryParameters.UserId, queryParameters.Status, queryParameters.CompetencyTypeId,
            cancellationToken);

        var dtos = competencies.Select(CompetencyMapper.ToDto).ToList();

        return Result<PagedResult<CompetencyDto>>.Success(
            PagedResult<CompetencyDto>.Create(dtos, totalCount, queryParameters));
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
        
        // Bruker sjekk
        ApplicationUser? targetUser = await userRepository.GetByIdIgnoringFiltersAsync(userId, cancellationToken);
        if (targetUser is null || !targetUser.IsActive || targetUser.DeletedAt is not null)
            return Result<CompetencyDto>.Failure(AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));
        
        // Sjekker om brukeren som kaller CreateAsync har tilattelse til å legge til kompetanse på targetUser
        if (!departmentScope.IsAllowed(targetUser.DepartmentId, Permissions.CompetenciesAll, 
                Permissions.CompetenciesReadSub))
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Forbidden,
                    "Du har ikke tilgang til å opprette kompetansebevis for brukere i denne avdelingen."));

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
        
        // Sjekker om brukeren som kaller UpdateAsync har tilattelse til å endre kompetansen satt på targetUser
        ApplicationUser? targetUser = competency.ApplicationUser;
        if (targetUser is null || 
            !departmentScope.IsAllowed(targetUser.DepartmentId, Permissions.CompetenciesAll, 
                Permissions.CompetenciesReadSub))
            return Result<CompetencyDto>.Failure(
                AppError.Create(ErrorCode.Forbidden,
                    "Du har ikke tilgang til å oppdatere kompetansebevis for brukere i denne avdelingen."));

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

            // Gi interceptoren forretningskontekst for revoke
            auditContext.SetActionOverride("competency.revoke");
            auditContext.SetReason(request.RevokedReason);
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
        
        ApplicationUser? targetUser = await userRepository.GetByIdIgnoringFiltersAsync(competency.UserId, 
            cancellationToken);
        if (targetUser is null ||
            !departmentScope.IsAllowed(targetUser.DepartmentId, Permissions.CompetenciesAll, 
                Permissions.CompetenciesReadSub))
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Forbidden,
                    "Du har ikke tilgang til å slette kompetansebevis for brukere i denne avdelingen."));
        
        await competencyRepository.SoftDeleteAsync(competency, cancellationToken);
        await competencyRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<ExpiringCompetencyDto>>> GetExpiringAsync(
        CompetencyExpiringQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        // Count og paginert henting gjøres i to steg siden GetExpiringAsync i repoet
        // returnerer IReadOnlyList, ikke IQueryable.
        // TODO: Vurder å legge til CountExpiringAsync i repoet for renere DB-nivå paginering.
        IReadOnlyList<Competency> allExpiring = await competencyRepository.GetExpiringAsync(
            queryParameters.UserId, queryParameters.DepartmentId, cancellationToken);

        var dtos = allExpiring
            .OrderBy(c => c.ExpiryDate)
            .Skip(queryParameters.Skip)
            .Take(queryParameters.PageSize)
            .Select(CompetencyMapper.ToExpiringDto)
            .ToList();

        return Result<PagedResult<ExpiringCompetencyDto>>.Success(
            PagedResult<ExpiringCompetencyDto>.Create(dtos, allExpiring.Count, queryParameters));
    }
}