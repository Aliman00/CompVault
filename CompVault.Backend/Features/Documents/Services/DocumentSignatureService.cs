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

    /// <inheritdoc />
    public async Task<Result<PagedResult<DocumentListDto>>> GetMySignedDocumentsAsync(
        Guid userId, PagedQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> signedDocumentIds = await signatureRepository.GetSignedDocumentIdsAsync(userId, cancellationToken);

        if (signedDocumentIds.Count == 0)
            return Result<PagedResult<DocumentListDto>>.Success(
                PagedResult<DocumentListDto>.Create([], 0, query));

        var documents = (await documentRepository.GetByIdsAsync(signedDocumentIds, cancellationToken))
            .OrderByDescending(d => d.UploadedAt)
            .ToList();

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(
            documents.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        var allDtos = DocumentMapper.MapToListDtos(documents, allSignatures, signedByCurrentUserOverride: true);

        // In-memory paginering — listen er per-bruker og typisk overkommelig
        var pagedDtos = allDtos
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToList();

        return Result<PagedResult<DocumentListDto>>.Success(
            PagedResult<DocumentListDto>.Create(pagedDtos, allDtos.Count, query));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentListDto>>> GetMyPendingDocumentsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<IReadOnlyList<DocumentListDto>>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));

        IReadOnlyList<Guid> signedDocumentIds = await signatureRepository.GetSignedDocumentIdsAsync(userId, cancellationToken);

        IReadOnlyList<Document> pendingDocuments = await documentRepository.GetPendingForUserAsync(
            userId, user.DepartmentId, user.JobTitleId, cancellationToken);

        pendingDocuments = pendingDocuments
            .Where(d => d.RequiresSignature && !signedDocumentIds.Contains(d.Id))
            .ToList();

        if (pendingDocuments.Count == 0)
            return Result<IReadOnlyList<DocumentListDto>>.Success(Array.Empty<DocumentListDto>());

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(
            pendingDocuments.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        List<DocumentListDto> dtos = DocumentMapper.MapToListDtos(pendingDocuments, allSignatures, signedByCurrentUserOverride: false);

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<DocumentProgressDto>> GetProgressAsync(
        string documentTypeSlug, Guid userId, CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);
        if (documentType is null)
            return Result<DocumentProgressDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<DocumentProgressDto>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));

        IReadOnlyList<Document> documents = await documentRepository.GetAccessibleByDocumentTypeAsync(
            documentType.Id, user.DepartmentId, user.JobTitleId, cancellationToken);

        // Kun dokumenter som krever signering er relevante for fremdrift
        var requiringSignature = documents.Where(d => d.RequiresSignature).ToList();
        int total = requiringSignature.Count;

        if (total == 0)
        {
            return Result<DocumentProgressDto>.Success(new DocumentProgressDto
            {
                Total = 0,
                Signed = 0,
                Pending = 0,
                PercentComplete = 0
            });
        }

        // Hent signaturer for disse dokumentene og sjekk mot gjeldende versjon.
        // Dette sikrer at signaturer på gamle versjoner ikke telles som "signert"
        // når dokumentet har fått ny versjon og krever ny signering.
        var docIds = requiringSignature.Select(d => d.Id).ToList();
        IReadOnlyList<DocumentSignature> allSignatures = await signatureRepository.GetByDocumentIdsAsync(
            docIds, cancellationToken);

        int signed = requiringSignature.Count(d =>
            allSignatures.Any(s =>
                s.DocumentId == d.Id &&
                s.UserId == userId &&
                s.SignatureVersion == d.Version));
        int pending = total - signed;

        return Result<DocumentProgressDto>.Success(new DocumentProgressDto
        {
            Total = total,
            Signed = signed,
            Pending = pending,
            PercentComplete = (int)Math.Round((double)signed / total * 100)
        });
    }
}