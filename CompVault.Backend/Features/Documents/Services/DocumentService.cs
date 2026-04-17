using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
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
    IJobTitleRepository jobTitleRepository,
    IUserRepository userRepository,
    IDocumentFileService documentFileService,
    ILogger<DocumentService> logger) : IDocumentService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentListDto>>> GetAllAsync(
        string documentTypeSlug,
        Guid? currentUserId,
        Guid? documentTypeCategoryId,
        bool bypassTargeting = false,
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

        // Filtrer på målgruppe hvis brukeren ikke har admin-bypass
        if (!bypassTargeting && currentUserId.HasValue)
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            Guid? userDeptId = user?.DepartmentId;
            Guid? userJobTitleId = user?.JobTitleId;

            documents = documents
                .Where(d => CanUserAccessDocument(d, userDeptId, userJobTitleId))
                .ToList();
        }

        // Batch-hent alle signaturer
        var docIds = documents.Select(d => d.Id).ToList();
        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(docIds, cancellationToken)).ToList();

        List<DocumentListDto> dtos = MapToListDtos(documents, allSignatures, currentUserId);

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> GetByIdAsync(
        Guid id, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetWithDetailsAsync(id, cancellationToken);

        if (document is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{id}' ble ikke funnet."));

        if (!bypassTargeting && currentUserId.HasValue)
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (!CanUserAccessDocument(document, user?.DepartmentId, user?.JobTitleId))
                return Result<DocumentDto>.Failure(
                    AppError.Create(ErrorCode.Forbidden, "Du har ikke tilgang til dette dokumentet."));
        }

        return Result<DocumentDto>.Success(DocumentMapper.ToDto(document));
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> CreateAsync(
        string documentTypeSlug,
        CreateDocumentRequest request,
        Guid uploadedById,
        bool bypassTarget,
        string? fileName = null,
        string? contentType = null,
        Stream? fileStream = null,
        CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetWithCategoriesBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        Result targetValidation = ValidateTarget(
            documentType, request.TargetDepartmentIds, request.TargetJobTitleIds, isCreate: true);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        // Sjekk at alle oppgitte avdelinger finnes og at brukere har tilattelse til å legge de til
        if (request.TargetDepartmentIds.Count > 0)
        {
            Result<IReadOnlyList<Department>> deptResult = await GetAndValidateDepartmentsExistAsync(
                uploadedById, request.TargetDepartmentIds, cancellationToken);
            if (deptResult.IsFailure)
                return Result<DocumentDto>.Failure(deptResult.Error!);
            IReadOnlyList<Department> allDepartments = deptResult.Value!;
            
            // Hopper over tilattelse sjekken hvis brukeren har riktig permission
            if (!bypassTarget)
            {
                ApplicationUser? user = await userRepository.GetByIdAsync(uploadedById, cancellationToken);
                if (user?.DepartmentId is null)
                {
                    logger.LogWarning("Bruker {UserId} har ingen tilknyttet avdeling", uploadedById);
                    return Result<DocumentDto>.Failure(
                        AppError.Create(ErrorCode.Forbidden, "Bruker har ingen tilknyttet avdeling"));
                }
                
                IReadOnlySet<Guid> allowedIds = GetDepartmentAndDescendantIds(allDepartments, user.DepartmentId.Value);
            
                var forbiddenIds = request.TargetDepartmentIds
                    .Where(id => !allowedIds.Contains(id))
                    .ToList();
                if (forbiddenIds.Count > 0)
                {
                    logger.LogWarning("Bruker {BrukerId} prøvde å legge til avdelinger uten tilattelse: {ForbiddenIds}", 
                        uploadedById, string.Join(", ", forbiddenIds));
                    return Result<DocumentDto>.Failure(
                        AppError.Create(ErrorCode.ForbiddenDepartment,
                            $"Du har ikke tilgang til følgende avdelinger: {string.Join(", ", forbiddenIds)}"));
                }
            }
        }

        // Sjekk at alle oppgitte jobbtitler finnes (batch-spørring)
        if (request.TargetJobTitleIds.Count > 0)
        {
            var existingJtIds = (await jobTitleRepository.FindAsync(
                j => request.TargetJobTitleIds.Contains(j.Id) && j.IsActive, cancellationToken))
                .Select(j => j.Id).ToHashSet();
            var missing = request.TargetJobTitleIds.Except(existingJtIds).ToList();
            if (missing.Count > 0)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Jobbtittel med ID '{missing.First()}' ble ikke funnet."));
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
            RequiresSignature = request.RequiresSignature,
            Version = 1,
            UploadedBy = uploadedById,
            IsActive = true,
            DocumentDepartments = request.TargetDepartmentIds
                .Select(id => new DocumentDepartment { DepartmentId = id }).ToList(),
            DocumentJobTitles = request.TargetJobTitleIds
                .Select(id => new DocumentJobTitle { JobTitleId = id }).ToList()
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
    public async Task<Result<DocumentDto>> UpdateAsync(Guid id, Guid userId, UpdateDocumentRequest request,
        bool bypassTarget, CancellationToken cancellationToken = default)
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

        // Finn effektive mål-lister for validering.
        // Null på request betyr "ikke endre", tom liste betyr "fjern alle".
        List<Guid> effectiveDepartmentIds = request.TargetDepartmentIds ?? document.DocumentDepartments.Select(dd => dd.DepartmentId).ToList();
        List<Guid> effectiveJobTitleIds = request.TargetJobTitleIds ?? document.DocumentJobTitles.Select(dj => dj.JobTitleId).ToList();

        Result targetValidation = ValidateTarget(documentType, effectiveDepartmentIds, effectiveJobTitleIds, isCreate: false);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        // Valider nye og fjernede avdelinger - sikrer at de finnes og at bruker endrer kun det de har tilattelse til
        if (request.TargetDepartmentIds is not null)
        {
            var currentDepartmentIds = document.DocumentDepartments
                .Select(d => d.DepartmentId).ToHashSet();
            var addedDepartmentIds = request.TargetDepartmentIds
                .Where(d => !currentDepartmentIds.Contains(d)).ToList();
            
            Result<IReadOnlyList<Department>> deptResult = await GetAndValidateDepartmentsExistAsync(
                userId, addedDepartmentIds, cancellationToken);
            if (deptResult.IsFailure)
                return Result<DocumentDto>.Failure(deptResult.Error!);
            IReadOnlyList<Department> allDepartments = deptResult.Value!;
            
            // Hopper over tilgangssjekken med riktig permission
            if (!bypassTarget)
            {
                ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);
                if (user?.DepartmentId is null)
                {
                    logger.LogWarning("Bruker {UserId} har ingen tilknyttet avdeling", userId);
                    return Result<DocumentDto>.Failure(
                        AppError.Create(ErrorCode.Forbidden, "Bruker har ingen tilknyttet avdeling"));
                }
                
                IReadOnlySet<Guid> allowedIds = GetDepartmentAndDescendantIds(allDepartments, user.DepartmentId.Value);
                
                // Lister for alle avdelinger som fjernes og er forbudte for brukeren
                var removedDepartmentIds = currentDepartmentIds
                    .Where(d => !request.TargetDepartmentIds.Contains(d)).ToList();
                var forbiddenIds = addedDepartmentIds.Concat(removedDepartmentIds)
                    .Where(guid => !allowedIds.Contains(guid))
                    .ToList();
            
                if (forbiddenIds.Count > 0)
                {
                    logger.LogWarning("Bruker {UserId} prøvde å endre avdelinger uten tilattelse: {ForbiddenIds}",
                        userId, string.Join(", ", forbiddenIds));
                    return Result<DocumentDto>.Failure(
                        AppError.Create(ErrorCode.ForbiddenDepartment,
                            $"Du har ikke tilgang til følgende avdelinger: {string.Join(", ", forbiddenIds)}"));
                }
            }
        }

        // Valider nye jobbtitler hvis oppgitt (batch-spørring)
        if (request.TargetJobTitleIds is not null && request.TargetJobTitleIds.Count > 0)
        {
            var existingJtIds = (await jobTitleRepository.FindAsync(
                j => request.TargetJobTitleIds.Contains(j.Id) && j.IsActive, cancellationToken))
                .Select(j => j.Id).ToHashSet();
            var missing = request.TargetJobTitleIds.Except(existingJtIds).ToList();
            if (missing.Count > 0)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Jobbtittel med ID '{missing.First()}' ble ikke funnet."));
        }

        if (request.DocumentTypeCategoryId.HasValue && !request.ClearDocumentTypeCategoryId)
        {
            DocumentType? documentTypeWithCategories = await documentTypeRepository.GetWithCategoriesBySlugAsync(documentType.Slug, cancellationToken);
            if (documentTypeWithCategories is null || documentTypeWithCategories.Categories.All(c => c.Id != request.DocumentTypeCategoryId.Value))
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Kategori med ID '{request.DocumentTypeCategoryId.Value}' finnes ikke for dokumentets dokumenttype."));
        }

        ApplyUpdate(document, request);

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

        // Målgruppe-sjekk: bruker må være i minst én av de valgte avdelingene
        // OG minst én av de valgte jobbtittlene (hvis begge er satt).
        if (document.DocumentDepartments.Count > 0 || document.DocumentJobTitles.Count > 0)
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<bool>.Failure(AppError.NotFound("Bruker ble ikke funnet."));

            bool departmentMatch = document.DocumentDepartments.Count == 0 ||
                document.DocumentDepartments.Any(dd => dd.DepartmentId == user.DepartmentId);

            bool jobTitleMatch = document.DocumentJobTitles.Count == 0 ||
                document.DocumentJobTitles.Any(dj => dj.JobTitleId == user.JobTitleId);

            if (!departmentMatch || !jobTitleMatch)
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

        if (!bypassTargeting && currentUserId.HasValue)
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (!CanUserAccessDocument(document, user?.DepartmentId, user?.JobTitleId))
                return Result<DocumentDownloadResult>.Failure(
                    AppError.Create(ErrorCode.Forbidden, "Du har ikke tilgang til dette dokumentet."));
        }

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
        Guid documentId, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetWithDetailsAsync(documentId, cancellationToken);

        if (document is null)
            return Result<IReadOnlyList<DocumentSignatureDto>>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        if (!bypassTargeting && currentUserId.HasValue)
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
            if (!CanUserAccessDocument(document, user?.DepartmentId, user?.JobTitleId))
                return Result<IReadOnlyList<DocumentSignatureDto>>.Failure(
                    AppError.Create(ErrorCode.Forbidden, "Du har ikke tilgang til dette dokumentet."));
        }

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
    /// </summary>
    private static void ApplyUpdate(Document document, UpdateDocumentRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Title))
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

        // Oppdater mål-avdelinger: null = ikke endret, liste = erstatt
        if (request.TargetDepartmentIds is not null)
        {
            document.DocumentDepartments = request.TargetDepartmentIds
                .Select(id => new DocumentDepartment { DocumentId = document.Id, DepartmentId = id })
                .ToList();
        }

        // Oppdater mål-jobbtitler: null = ikke endret, liste = erstatt
        if (request.TargetJobTitleIds is not null)
        {
            document.DocumentJobTitles = request.TargetJobTitleIds
                .Select(id => new DocumentJobTitle { DocumentId = document.Id, JobTitleId = id })
                .ToList();
        }
    }

    /// <summary>
    /// Validerer at target-listene er konsistente med dokumenttypens TargetMode.
    /// Ved opprettelse (isCreate=true) kreves at påkrevde lister har minst ett element.
    /// Ved oppdatering (isCreate=false) sjekkes kun at regler ikke brytes.
    /// </summary>
    private static Result ValidateTarget(
        DocumentType documentType,
        List<Guid> targetDepartmentIds,
        List<Guid> targetJobTitleIds,
        bool isCreate)
    {
        bool hasDepartments = targetDepartmentIds.Count > 0;
        bool hasJobTitles = targetJobTitleIds.Count > 0;

        return documentType.TargetMode switch
        {
            DocumentTargetMode.None when hasDepartments || hasJobTitles =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' har TargetMode=None. Target-lister kan ikke settes.")),
            DocumentTargetMode.Department when isCreate && !hasDepartments =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever minst én målavdeling (TargetDepartmentIds).")),
            DocumentTargetMode.Department when hasJobTitles && !hasDepartments =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetDepartmentIds er satt når TargetJobTitleIds brukes.")),
            DocumentTargetMode.JobTitle when isCreate && !hasJobTitles =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever minst én mål-jobbtittel (TargetJobTitleIds).")),
            DocumentTargetMode.JobTitle when hasDepartments && !hasJobTitles =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetJobTitleIds er satt når TargetDepartmentIds brukes.")),
            _ => Result.Success()
        };
    }

    /// <summary>
    /// Sjekker om en bruker har tilgang til et dokument basert på målgruppe.
    /// TargetMode None = alle kan se. Department/JobTitle = bruker må matche minst én i listen.
    /// Hvis begge lister er satt, må brukeren matche minst én i HVER liste (AND-logikk mellom kategorier).
    /// </summary>
    private static bool CanUserAccessDocument(
        Document document, Guid? userDepartmentId, Guid? userJobTitleId)
    {
        // Ingen målgruppe = alle kan se
        if (document.DocumentDepartments.Count == 0 && document.DocumentJobTitles.Count == 0)
            return true;

        // Hvis avdelingsmålgruppe er satt, må brukeren matche minst én avdeling
        bool departmentMatch = document.DocumentDepartments.Count == 0 ||
            (userDepartmentId.HasValue && document.DocumentDepartments.Any(dd => dd.DepartmentId == userDepartmentId.Value));

        // Hvis jobbtittel-målgruppe er satt, må brukeren matche minst én jobbtittel
        bool jobTitleMatch = document.DocumentJobTitles.Count == 0 ||
            (userJobTitleId.HasValue && document.DocumentJobTitles.Any(dj => dj.JobTitleId == userJobTitleId.Value));

        // AND-logikk mellom kategoriene: hvis begge er satt, må begge matche
        return departmentMatch && jobTitleMatch;
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
    
    // Hjelpemetode for å sjekke at en avdeling eksisterer
    private async Task<Result<IReadOnlyList<Department>>> GetAndValidateDepartmentsExistAsync(
        Guid userId, List<Guid> departmentIdsToValidate, CancellationToken ct)
    {
        IReadOnlyList<Department> allDepartments =
            await departmentRepository.GetAllWithHierarchyAsync(ct);

        var existingDepartmentIds = allDepartments.Select(d => d.Id).ToHashSet();
        var missingIds = departmentIdsToValidate
            .Where(id => !existingDepartmentIds.Contains(id))
            .ToList();

        if (missingIds.Count > 0)
        {
            logger.LogWarning("Bruker {UserId} prøvde å legge til avdeling {DepartmetnId} som ikke finnes", userId,
                missingIds.First());
            return Result<IReadOnlyList<Department>>.Failure(
                AppError.NotFound($"Avdeling med ID '{missingIds.First()}' ble ikke funnet."));
        }
        
        return Result<IReadOnlyList<Department>>.Success(allDepartments);
    }
    
    // Hjelpemetode for å finne alle underavdelingene under innsendt avdeling
    private static IReadOnlySet<Guid> GetDepartmentAndDescendantIds(IReadOnlyList<Department> allDepartments,
        Guid departmentId)
    {
        var allowedDepartments = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(departmentId);
        
        // Går igjennom alle avdelinger nedover hierarkiet rekursivt og legger alle avdelingene under
        // innsendt avdeling til i HashSet
        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            allowedDepartments.Add(current);

            foreach (Department childDepartment in allDepartments.Where(d => d.ParentDepartmentId == current))
            {
                queue.Enqueue(childDepartment.Id);
            }
        }

        return allowedDepartments;
    }
}