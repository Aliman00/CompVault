using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Documents.Services;

/// <inheritdoc />
public sealed class DocumentSignatureService(
    IDocumentRepository documentRepository,
    IDocumentSignatureRepository signatureRepository,
    IDocumentTypeRepository documentTypeRepository,
    IUserRepository userRepository,
    IDocumentTargetingService targetingService,
    ILogger<DocumentSignatureService> logger) : IDocumentSignatureService
{
    /// <inheritdoc />
    public async Task<Result<bool>> SignAsync(
        Guid documentId, Guid userId, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetCurrentWithSignaturesAsync(documentId, cancellationToken);

        if (document is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        if (!document.RequiresSignature)
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Validation, "Dette dokumentet krever ikke signering."));

        Result accessResult = await targetingService.CheckAccessAsync(document, userId, bypassTargeting: false, cancellationToken);
        if (accessResult.IsFailure)
            return Result<bool>.Failure(accessResult.Error!);

        bool alreadySigned = await signatureRepository.HasUserSignedVersionAsync(
            documentId, userId, document.Version, cancellationToken);

        if (alreadySigned)
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Conflict, "Du har allerede signert denne versjonen av dokumentet."));

        var signature = new DocumentSignature
        {
            DocumentId = documentId,
            UserId = userId,
            SignatureVersion = document.Version
        };

        try
        {
            await signatureRepository.AddAsync(signature, cancellationToken);
            await signatureRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique constraint på (DocumentId, UserId, SignatureVersion) beskytter mot
            // doble signaturer ved concurrent requests
            logger.LogWarning("Signering av dokument {DocumentId} av bruker {UserId} feilet — muligens allerede signert",
                documentId, userId);
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Conflict, "Du har allerede signert denne versjonen av dokumentet."));
        }

        logger.LogInformation("Bruker {UserId} signerte dokument {DocumentId} versjon {Version}",
            userId, documentId, document.Version);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserSignatureStatusDto>>> GetSignatureStatusAsync(
        Guid documentId, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetWithDetailsAsync(documentId, cancellationToken);

        if (document is null)
            return Result<IReadOnlyList<UserSignatureStatusDto>>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));
        
        // Sjekker at brukeren har lov til å se dokumentet
        Result accessResult = await targetingService.CheckAccessAsync(document, currentUserId, bypassTargeting, 
            cancellationToken);
        if (accessResult.IsFailure)
            return Result<IReadOnlyList<UserSignatureStatusDto>>.Failure(accessResult.Error!);
        
        // Henter ut avdelingene og jobbstillingene hvis noen er i målgruppen
        var departmentIds = document.DocumentDepartments.Select(dd => dd.DepartmentId).ToList();
        var jobTitleIds = document.DocumentJobTitles.Select(dj => dj.JobTitleId).ToList();

        // Hent signaturer og målgruppebrukere - ikke kjør paralellt siden de er innom samme tabell
        IReadOnlyList<DocumentSignature> signatures = await signatureRepository.GetForDocumentVersionAsync(
            documentId, document.Version, cancellationToken);

        IReadOnlyList<ApplicationUser> targetedUsers = await userRepository.GetUsersByTargetAsync(
            departmentIds, jobTitleIds, cancellationToken);
        
        // Slår sammen signaturen og brukeren til SignatureStatusDto og sorterer etter om brukeren har signert
        var dtos = targetedUsers
            .Select(u => DocumentMapper.ToSignatureStatusDto(
                u, signatures.FirstOrDefault(s => s.UserId == u.Id)))
            .OrderBy(u => u.HasSigned)
            .ThenBy(u => u.FullName)
            .ToList();

        return Result<IReadOnlyList<UserSignatureStatusDto>>.Success(dtos);
    }
}