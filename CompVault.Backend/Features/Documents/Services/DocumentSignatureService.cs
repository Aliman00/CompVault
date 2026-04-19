using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Documents.Services;

/// <inheritdoc />
public sealed class DocumentSignatureService(
    IDocumentRepository documentRepository,
    IDocumentSignatureRepository signatureRepository,
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
    public async Task<Result<IReadOnlyList<DocumentSignatureDto>>> GetSignaturesAsync(
        Guid documentId, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetWithDetailsAsync(documentId, cancellationToken);

        if (document is null)
            return Result<IReadOnlyList<DocumentSignatureDto>>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        Result accessResult = await targetingService.CheckAccessAsync(document, currentUserId, bypassTargeting, cancellationToken);
        if (accessResult.IsFailure)
            return Result<IReadOnlyList<DocumentSignatureDto>>.Failure(accessResult.Error!);

        // Hent signaturer for dokumentets gjeldende versjon
        int currentVersion = document.Version;
        IReadOnlyList<DocumentSignature> signatures = await signatureRepository.GetForDocumentVersionAsync(
            documentId, currentVersion, cancellationToken);

        var dtos = signatures.Select(DocumentMapper.ToSignatureDto).ToList();
        return Result<IReadOnlyList<DocumentSignatureDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentListDto>>> GetMySignedDocumentsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> signedDocumentIds = await signatureRepository.GetSignedDocumentIdsAsync(userId, cancellationToken);

        if (signedDocumentIds.Count == 0)
            return Result<IReadOnlyList<DocumentListDto>>.Success(Array.Empty<DocumentListDto>());

        var documents = (await documentRepository.GetByIdsAsync(signedDocumentIds, cancellationToken))
            .ToList();

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(
            documents.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        List<DocumentListDto> dtos = DocumentMapper.MapToListDtos(documents, allSignatures, signedByCurrentUserOverride: true);

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
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
}