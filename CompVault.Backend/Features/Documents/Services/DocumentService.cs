using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Documents.Services;

/// <inheritdoc />
public sealed class DocumentService(
    IDocumentRepository documentRepository,
    IDocumentSignatureRepository signatureRepository,
    IDocumentTypeRepository documentTypeRepository,
    IDepartmentRepository departmentRepository,
    IUserRepository userRepository,
    IDocumentFileService documentFileService,
    ILogger<DocumentService> logger) : IDocumentService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentListDto>>> GetAllAsync(
        string documentTypeSlug,
        Guid? currentUserId,
        Guid? documentTypeCategoryId,
        CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<IReadOnlyList<DocumentListDto>>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        IReadOnlyList<Document> documents = await documentRepository.GetByDocumentTypeAsync(
            documentType.Id, documentTypeCategoryId, cancellationToken);

        if (documents.Count == 0)
            return Result<IReadOnlyList<DocumentListDto>>.Success(Array.Empty<DocumentListDto>());

        // Batch-hent alle signaturer
        var docIds = documents.Select(d => d.Id).ToList();
        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(docIds, cancellationToken)).ToList();

        List<DocumentListDto> dtos = MapToListDtos(documents, allSignatures, currentUserId);

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetWithDetailsAsync(id, cancellationToken);

        if (document is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{id}' ble ikke funnet."));

        return Result<DocumentDto>.Success(DocumentMapper.ToDto(document));
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> CreateAsync(
        string documentTypeSlug,
        CreateDocumentRequest request,
        Guid uploadedById,
        string? fileName = null,
        string? contentType = null,
        Stream? fileStream = null,
        CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetWithCategoriesBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        Result targetValidation = ValidateTarget(documentType, request.TargetDepartmentId, request.TargetJobTitleId, isCreate: true);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        if (request.TargetDepartmentId.HasValue)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.TargetDepartmentId.Value && d.IsActive, cancellationToken);

            if (!departmentExists)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.TargetDepartmentId.Value}' ble ikke funnet."));
        }

        // Categories er alltid lastet via GetWithCategoriesBySlugAsync
        if (request.DocumentTypeCategoryId.HasValue && documentType.Categories.All(c => c.Id != request.DocumentTypeCategoryId.Value))
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Kategori med ID '{request.DocumentTypeCategoryId.Value}' finnes ikke for denne dokumenttypen."));

        var document = new Document
        {
            DocumentTypeId = documentType.Id,
            DocumentTypeCategoryId = request.DocumentTypeCategoryId,
            Title = request.Title,
            Description = request.Description,
            ExternalUrl = request.ExternalUrl,
            TargetDepartmentId = request.TargetDepartmentId,
            TargetJobTitleId = request.TargetJobTitleId,
            RequiresSignature = request.RequiresSignature,
            Version = 1,
            UploadedBy = uploadedById,
            IsActive = true
        };

        // Håndter filopplasting med opprydding ved DB-feil
        string? savedFilePath = null;

        if (fileStream is not null && fileName is not null && contentType is not null)
        {
            Result mimeTypeResult = documentFileService.ValidateMimeType(contentType, documentType.AllowedMimeTypes);
            if (mimeTypeResult.IsFailure)
                return Result<DocumentDto>.Failure(mimeTypeResult.Error!);

            Result sizeResult = documentFileService.ValidateFileSize(fileStream.Length, documentType.MaxFileSizeBytes);
            if (sizeResult.IsFailure)
                return Result<DocumentDto>.Failure(sizeResult.Error!);

            string extension = Path.GetExtension(fileName);
            string newFilePath = $"{documentType.StorageFolder}/active/{document.Id}/file_v1{extension}";

            (string? filePath, string? checksum) = await documentFileService.SaveWithChecksumAsync(fileStream, newFilePath, cancellationToken);
            savedFilePath = filePath;

            document.FileName = fileName;
            document.FilePath = filePath;
            document.FileSize = fileStream.Length;
            document.MimeType = contentType;
            document.Checksum = checksum;
        }

        await documentRepository.AddAsync(document, cancellationToken);

        try
        {
            await documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Rydd opp fil hvis DB-lagring feiler
            logger.LogError("DB-feil ved opprettelse av dokument — fil ryddet opp: {FilePath}", savedFilePath);
            if (savedFilePath is not null)
                await documentFileService.DeleteAsync(savedFilePath, CancellationToken.None);

            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke lagre dokumentet. Prøv på nytt."));
        }

        Document? created = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken);

        if (created is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{document.Id}' ble ikke funnet etter opprettelse."));

        logger.LogInformation("Dokument {DocumentId} opprettet (type: {DocumentTypeSlug}, versjon: 1)",
            document.Id, documentTypeSlug);

        return Result<DocumentDto>.Success(DocumentMapper.ToDto(created));
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> UpdateAsync(
        Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetForUpdateAsync(id, cancellationToken);

        if (document is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{id}' ble ikke funnet."));

        DocumentType? documentType = document.DocumentType;
        if (documentType is null)
        {
            DocumentType? fetched = await documentTypeRepository.GetByIdAsync(document.DocumentTypeId, cancellationToken);
            if (fetched is null)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound("Dokumenttype for dokumentet ble ikke funnet."));
            documentType = fetched;
        }

        // Beregn verdiene som faktisk skal valideres.
        // ClearFlags betyr "nullstill feltet", ikke "sett feltet" — derfor filtrerer vi
        // ut verdier som bare er sendt for å nullstilles, og sender kun de som faktisk skal settes.
        bool wantsTargetDepartment = request.TargetDepartmentId.HasValue && !request.ClearTargetDepartmentId;
        bool wantsTargetJobTitle = request.TargetJobTitleId.HasValue && !request.ClearTargetJobTitleId;
        Guid? effectiveDepartmentId = wantsTargetDepartment ? request.TargetDepartmentId : null;
        Guid? effectiveJobTitleId = wantsTargetJobTitle ? request.TargetJobTitleId : null;

        Result targetValidation = ValidateTarget(documentType, effectiveDepartmentId, effectiveJobTitleId, isCreate: false);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        if (request.TargetDepartmentId.HasValue && !request.ClearTargetDepartmentId)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.TargetDepartmentId.Value && d.IsActive, cancellationToken);

            if (!departmentExists)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.TargetDepartmentId.Value}' ble ikke funnet."));
        }

        if (request.DocumentTypeCategoryId.HasValue && !request.ClearDocumentTypeCategoryId)
        {
            DocumentType? documentTypeWithCategories = await documentTypeRepository.GetWithCategoriesBySlugAsync(documentType.Slug, cancellationToken);
            if (documentTypeWithCategories is null || documentTypeWithCategories.Categories.All(c => c.Id != request.DocumentTypeCategoryId.Value))
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Kategori med ID '{request.DocumentTypeCategoryId.Value}' finnes ikke for dokumentets dokumenttype."));
        }

        ApplyUpdate(document, request, effectiveDepartmentId, effectiveJobTitleId);

        try
        {
            await documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke lagre dokumentendringene. Prøv på nytt."));
        }

        Document? updated = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken);

        if (updated is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{id}' ble ikke funnet etter oppdatering."));

        logger.LogInformation("Dokument {DocumentId} oppdatert", id);

        return Result<DocumentDto>.Success(DocumentMapper.ToDto(updated));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetByIdAsync(id, cancellationToken);

        if (document is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Dokument med ID '{id}' ble ikke funnet."));

        await documentRepository.SoftDeleteAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Dokument {DocumentId} slettet (soft delete)", id);

        return Result<bool>.Success(true);
    }

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

        if (document.TargetDepartmentId.HasValue || document.TargetJobTitleId.HasValue)
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<bool>.Failure(AppError.NotFound("Bruker ble ikke funnet."));

            bool inTargetGroup =
                (document.TargetDepartmentId is null || document.TargetDepartmentId == user.DepartmentId) &&
                (!document.TargetJobTitleId.HasValue || document.TargetJobTitleId == user.JobTitleId);

            if (!inTargetGroup)
                return Result<bool>.Failure(
                    AppError.Create(ErrorCode.Forbidden, "Du tilhører ikke målgruppen for dette dokumentet."));
        }

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
        // Disk-operasjoner gjøres først. Hvis noe feiler senere kan temp-filen ryddes.
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

        // Beregn arkiv-sti for gamle filen — denne pathen lagres i DB
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
        try
        {
            await documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            logger.LogError("DB-feil ved lagring av dokumentversjon {DocumentId} v{Version} — temp-fil ryddet opp",
                documentId, newVersion);
            // DB feilet — temp-fil er eneste som ligger på disk, rydd opp
            await documentFileService.DeleteAsync(tempFilePath, CancellationToken.None);
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke lagre dokumentversjonen. Prøv på nytt."));
        }

        // ---- Fase 4: Flytt filer på disk etter vellykket DB-commit ----
        // Hvis en flytting feiler her, er DB konsistent med nye metadata.
        // Gammel fil ligger fortsatt på sin opprinnelige path, og temp-fil ligger på temp-path.
        // Begge er sikre — ingen data er tapt. Kan ryddes opp manuelt eller via cron.
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
            // Ikke returner feil — DB er konsistent, filen ligger på temp og kan flyttes manuelt.
            // Brukeren får dokumentet, men fil-tilgang vil feile inntil filen flyttes.
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
        Guid documentId, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetByIdAsync(documentId, cancellationToken);

        if (document is null)
            return Result<DocumentDownloadResult>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

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
    Task<Stream> IDocumentService.OpenFileStreamAsync(string relativePath, CancellationToken cancellationToken)
        => documentFileService.OpenReadAsync(relativePath, cancellationToken);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentSignatureDto>>> GetSignaturesAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetByIdAsync(documentId, cancellationToken);

        if (document is null)
            return Result<IReadOnlyList<DocumentSignatureDto>>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        IReadOnlyList<DocumentSignature> signatures = await signatureRepository.GetForDocumentVersionAsync(
            documentId, document.Version, cancellationToken);

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

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(documents.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        List<DocumentListDto> dtos = MapToListDtos(documents, allSignatures, signedByCurrentUserOverride: true);

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

        if (pendingDocuments is null || pendingDocuments.Count == 0)
            return Result<IReadOnlyList<DocumentListDto>>.Success(Array.Empty<DocumentListDto>());

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(
            pendingDocuments.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        List<DocumentListDto> dtos = MapToListDtos(pendingDocuments, allSignatures, signedByCurrentUserOverride: false);

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
    }

    /// <summary>
    /// Apllierer oppdateringer fra DTO på dokumententiteten.
    /// Bruker effective-verdier for target-felt for å sikre konsistens med valideringen.
    /// </summary>
    private static void ApplyUpdate(
        Document document,
        UpdateDocumentRequest request,
        Guid? effectiveDepartmentId,
        Guid? effectiveJobTitleId)
    {
        if (!string.IsNullOrEmpty(request.Title))
            document.Title = request.Title;

        if (request.Description is not null)
            document.Description = request.Description;

        if (request.ClearDocumentTypeCategoryId)
            document.DocumentTypeCategoryId = null;
        else if (request.DocumentTypeCategoryId.HasValue)
            document.DocumentTypeCategoryId = request.DocumentTypeCategoryId.Value;

        if (request.RequiresSignature.HasValue)
            document.RequiresSignature = request.RequiresSignature.Value;

        if (request.ClearExternalUrl)
            document.ExternalUrl = null;
        else if (request.ExternalUrl is not null)
            document.ExternalUrl = request.ExternalUrl;

        // Bruk effective-verdier istedenfor rå request — sikrer konsistens med ValidateTarget
        document.TargetDepartmentId = effectiveDepartmentId;
        document.TargetJobTitleId = effectiveJobTitleId;
    }

    /// <summary>
    /// Validerer at target-feltene er konsistente med dokumenttypens TargetMode.
    /// Ved opprettelse (isCreate=true) kreves at påkrevde felt er satt.
    /// Ved oppdatering (isCreate=false) sjekkes kun at regler ikke brytes.
    /// </summary>
    private static Result ValidateTarget(
        DocumentType documentType,
        Guid? targetDepartmentId,
        Guid? targetJobTitleId,
        bool isCreate)
    {
        bool hasDepartment = targetDepartmentId.HasValue;
        bool hasJobTitle = targetJobTitleId.HasValue;

        return documentType.TargetMode switch
        {
            DocumentTargetMode.None when hasDepartment || hasJobTitle =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' har TargetMode=None. Target-felt kan ikke settes.")),
            DocumentTargetMode.Department when isCreate && !hasDepartment =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetDepartmentId er satt.")),
            DocumentTargetMode.Department when hasJobTitle && !hasDepartment =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetDepartmentId er satt når TargetJobTitleId brukes.")),
            DocumentTargetMode.JobTitle when isCreate && !hasJobTitle =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetJobTitleId er satt.")),
            _ => Result.Success()
        };
    }

    /// <summary>
    /// Kartlegger en liste dokumenter til DocumentListDto med signaturstatistikk.
    /// Centraliserer logikken for å telle signaturer og sjekke om brukeren har signert.
    /// </summary>
    private static List<DocumentListDto> MapToListDtos(
        IReadOnlyList<Document> documents,
        IReadOnlyList<DocumentSignature> allSignatures,
        Guid? currentUserId = null,
        bool? signedByCurrentUserOverride = null)
    {
        var dtos = new List<DocumentListDto>(documents.Count);

        foreach (Document doc in documents)
        {
            int signatureCount = allSignatures.Count(
                s => s.DocumentId == doc.Id && s.SignatureVersion == doc.Version);

            bool signedByCurrentUser = signedByCurrentUserOverride ?? currentUserId.HasValue &&
                allSignatures.Any(s =>
                    s.DocumentId == doc.Id &&
                    s.SignatureVersion == doc.Version &&
                    s.UserId == currentUserId.Value);

            dtos.Add(DocumentMapper.ToListDto(doc, signatureCount, signedByCurrentUser));
        }

        return dtos;
    }
}