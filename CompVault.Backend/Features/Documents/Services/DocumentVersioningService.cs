using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Features.Audit.Services;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Documents.Services;

/// <inheritdoc />
public sealed class DocumentVersioningService(
    IDocumentRepository documentRepository,
    IDocumentTypeRepository documentTypeRepository,
    IDocumentSignatureRepository signatureRepository,
    IDocumentTargetingService targetingService,
    IDocumentFileService documentFileService,
    IAuditContext auditContext,
    ILogger<DocumentVersioningService> logger) : IDocumentVersioningService
{
    /// <inheritdoc />
    public async Task<Result<DocumentDto>> UploadVersionAsync(
        Guid documentId,
        string documentTypeSlug,
        string fileName,
        string contentType,
        Stream stream,
        Guid uploadedById,
        CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        Document? document = await documentRepository.GetForUpdateAsync(documentId, cancellationToken);

        if (document is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        if (document.DocumentTypeId != documentType.Id)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' er ikke av dokumenttype '{documentTypeSlug}'."));

        Result mimeTypeResult = documentFileService.ValidateMimeType(contentType, documentType.AllowedMimeTypes);
        if (mimeTypeResult.IsFailure)
            return Result<DocumentDto>.Failure(mimeTypeResult.Error!);

        Result sizeResult = documentFileService.ValidateFileSize(stream.Length, documentType.MaxFileSizeBytes);
        if (sizeResult.IsFailure)
            return Result<DocumentDto>.Failure(sizeResult.Error!);

        // ---- Fase 1: Skriv ny fil til disk (temp-plassering) ----
        string extension = Path.GetExtension(fileName);
        string storageFolder = documentType.StorageFolder;
        string tempPath = $"{storageFolder}/active/{documentId}/file_v{document.Version + 1}_tmp{extension}";

        (string? tempFilePath, string? newChecksum) = await documentFileService.SaveWithChecksumAsync(stream, tempPath, cancellationToken);

        if (document.Checksum is not null && document.Checksum == newChecksum)
        {
            await documentFileService.DeleteAsync(tempFilePath, cancellationToken);
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Filinnholdet er identisk med forrige versjon. Ingen ny versjon opprettet."));
        }

        // ---- Fase 2: Forbered alle DB-endringer i minnet ----
        int newVersion = document.Version + 1;
        string newFilePath = $"{storageFolder}/active/{documentId}/file_v{newVersion}{extension}";

        // Ta vare på eksisterende filmetadata før oppdatering
        string? oldFilePath = document.FilePath;
        string? oldFileName = document.FileName;
        string? oldMimeType = document.MimeType;
        long? oldFileSize = document.FileSize;
        string? oldChecksum = document.Checksum;

        // Beregn arkiv-sti for gamle filen
        string? archivedPath = !string.IsNullOrEmpty(oldFilePath) && oldFilePath != newFilePath
            ? $"{storageFolder}/archived/{documentId}/file_v{newVersion - 1}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(oldFileName ?? fileName)}"
            : null;

        // Oppdater dokumentmetadata
        document.Version = newVersion;
        document.FileName = fileName;
        document.FilePath = newFilePath;
        document.FileSize = stream.Length;
        document.MimeType = contentType;
        document.Checksum = newChecksum;
        document.UploadedBy = uploadedById;
        document.UploadedAt = DateTime.UtcNow;

        // Slett signaturer via tracked delete
        IReadOnlyList<DocumentSignature> existingSignatures = await signatureRepository.GetForDocumentAsync(documentId, cancellationToken);
        foreach (DocumentSignature sig in existingSignatures)
            signatureRepository.Remove(sig);

        // Opprett versjonsrecord i samme context — alt lagres atomisk
        if (archivedPath is not null)
        {
            var versionRecord = new DocumentVersion
            {
                DocumentId = documentId,
                Version = newVersion - 1,
                FileName = oldFileName,
                FilePath = archivedPath,
                FileSize = oldFileSize,
                MimeType = oldMimeType,
                Checksum = oldChecksum,
                ArchivedAt = DateTime.UtcNow
            };
            await documentRepository.AddVersionAsync(versionRecord, cancellationToken);
        }

        // ---- Fase 3: Persistér alt i ETT SaveChanges ----
        // Gi interceptoren kontekst: dokumentversjon oppdatering, ikke vanlig update
        auditContext.SetActionOverride("document.upload_version");

        try
        {
            await documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            logger.LogError("DB-feil ved lagring av dokumentversjon {DocumentId} v{Version} — temp-fil ryddet opp",
                documentId, newVersion);
            await documentFileService.DeleteAsync(tempFilePath, CancellationToken.None);
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke lagre dokumentversjonen. Prøv på nytt."));
        }

        // ---- Fase 4: Flytt filer på disk etter vellykket DB-commit ----
        if (!string.IsNullOrEmpty(oldFilePath) && oldFilePath != newFilePath)
        {
            try
            {
                await documentFileService.MoveAsync(oldFilePath, archivedPath!, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Kunne ikke flytte gammel fil til arkiv for dokument {DocumentId}. Fil ligger på: {OldPath}",
                    documentId, oldFilePath);
            }
        }

        try
        {
            await documentFileService.MoveAsync(tempFilePath, newFilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kunne ikke flytte temp-fil til endelig plassering for dokument {DocumentId}. Fil ligger på: {TempPath}",
                documentId, tempFilePath);
        }

        Document? updated = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken);

        if (updated is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{document.Id}' ble ikke funnet etter opplasting."));

        logger.LogInformation("Dokument {DocumentId} versjon {Version} lastet opp av bruker {UserId}",
            document.Id, newVersion, uploadedById);

        return Result<DocumentDto>.Success(DocumentMapper.ToDto(updated));
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDownloadResult>> GetDownloadAsync(
        Guid documentId, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetWithDetailsAsync(documentId, cancellationToken);

        if (document is null)
            return Result<DocumentDownloadResult>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        Result accessResult = await targetingService.CheckAccessAsync(document, currentUserId, bypassTargeting, cancellationToken);
        if (accessResult.IsFailure)
            return Result<DocumentDownloadResult>.Failure(accessResult.Error!);

        if (string.IsNullOrEmpty(document.FilePath))
            return Result<DocumentDownloadResult>.Failure(
                AppError.Create(ErrorCode.Validation, "Dokumentet har ingen filvedlegg. Kun ekstern lenke er tilgjengelig."));

        bool fileExists = await documentFileService.ExistsAsync(document.FilePath, cancellationToken);

        if (!fileExists)
            return Result<DocumentDownloadResult>.Failure(
                AppError.NotFound($"Filen for dokument med ID '{documentId}' ble ikke funnet på lagring."));

        var result = new DocumentDownloadResult
        {
            FilePath = document.FilePath,
            FileName = document.FileName ?? "dokument.pdf",
            ContentType = document.MimeType ?? "application/octet-stream",
            FileSize = document.FileSize
        };

        return Result<DocumentDownloadResult>.Success(result);
    }

    /// <inheritdoc />
    public Task<Stream> OpenFileStreamAsync(string relativePath, CancellationToken cancellationToken = default)
        => documentFileService.OpenReadAsync(relativePath, cancellationToken);
}