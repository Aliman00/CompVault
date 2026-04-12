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
    IDocumentFileService documentFileService) : IDocumentService
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

        var dtos = new List<DocumentListDto>();
        foreach (Document doc in documents)
        {
            int signatureCount = allSignatures.Count(s =>
                s.DocumentId == doc.Id && s.SignatureVersion == doc.Version);

            bool signedByCurrentUser = currentUserId.HasValue && allSignatures.Any(s =>
                s.DocumentId == doc.Id && s.SignatureVersion == doc.Version && s.UserId == currentUserId.Value);

            dtos.Add(DocumentMapper.ToListDto(doc, signatureCount, signedByCurrentUser));
        }

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

        Result targetValidation = ValidateTarget(documentType, request.TargetDepartmentId, request.TargetJobTitle, isCreate: true);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        if (request.TargetDepartmentId.HasValue)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.TargetDepartmentId.Value, cancellationToken);

            if (!departmentExists)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.TargetDepartmentId.Value}' ble ikke funnet."));
        }

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
            TargetJobTitle = request.TargetJobTitle,
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
            if (savedFilePath is not null)
                await documentFileService.DeleteAsync(savedFilePath, CancellationToken.None);

            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke lagre dokumentet. Prøv på nytt."));
        }

        Document? created = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken);

        if (created is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{document.Id}' ble ikke funnet etter opprettelse."));

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

        bool wantsTargetDepartment = request.TargetDepartmentId.HasValue && !request.ClearTargetDepartment;
        bool wantsTargetJobTitle = !string.IsNullOrEmpty(request.TargetJobTitle) && !request.ClearTargetJobTitle;

        // Ved oppdatering sendes kun de verdiene som faktisk skal settes;
        // ClearFlags-tømming av felt sjekkes ikke her fordi de nullstiller til null/empty
        Guid? effectiveDepartmentId = wantsTargetDepartment ? request.TargetDepartmentId : null;
        string? effectiveJobTitle = wantsTargetJobTitle ? request.TargetJobTitle : null;

        Result targetValidation = ValidateTarget(documentType, effectiveDepartmentId, effectiveJobTitle, isCreate: false);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        if (request.TargetDepartmentId.HasValue && !request.ClearTargetDepartment)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.TargetDepartmentId.Value, cancellationToken);

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

        if (document.TargetDepartmentId.HasValue || !string.IsNullOrEmpty(document.TargetJobTitle))
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<bool>.Failure(AppError.NotFound("Bruker ble ikke funnet."));

            bool inTargetGroup =
                (document.TargetDepartmentId is null || document.TargetDepartmentId == user.DepartmentId) &&
                (string.IsNullOrEmpty(document.TargetJobTitle) || document.TargetJobTitle == user.JobTitle);

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
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Conflict, "Du har allerede signert denne versjonen av dokumentet."));
        }

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

        string extension = Path.GetExtension(fileName);
        string storageFolder = documentType.StorageFolder;
        string tempPath = $"{storageFolder}/active/{documentId}/file_v{document.Version + 1}_tmp{extension}";

        // Lagre fil og beregn sjekksum før DB-endringer
        (string? tempFilePath, string? newChecksum) = await documentFileService.SaveWithChecksumAsync(stream, tempPath, cancellationToken);

        if (document.Checksum is not null && document.Checksum == newChecksum)
        {
            await documentFileService.DeleteAsync(tempFilePath, cancellationToken);
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Filinnholdet er identisk med forrige versjon. Ingen ny versjon opprettet."));
        }

        int newVersion = document.Version + 1;
        string newFilePath = $"{storageFolder}/active/{documentId}/file_v{newVersion}{extension}";

        // Ta vare på eksisterende filmetadata før oppdatering
        string? oldFilePath = document.FilePath;
        string? oldFileName = document.FileName;
        string? oldMimeType = document.MimeType;
        long? oldFileSize = document.FileSize;
        string? oldChecksum = document.Checksum;

        // Oppdater dokumentmetadata i minnet
        document.Version = newVersion;
        document.FileName = fileName;
        document.FilePath = newFilePath;
        document.FileSize = stream.Length;
        document.MimeType = contentType;
        document.Checksum = newChecksum;
        document.UploadedBy = uploadedById;
        document.UploadedAt = DateTime.UtcNow;

        // Slett signaturer — ny versjon krever re-signering
        await signatureRepository.DeleteAllForDocumentAsync(documentId, cancellationToken);

        try
        {
            await documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // DB-feil etter at fil allerede er lagret — rydd opp temp-fil og returner feil
            await documentFileService.DeleteAsync(tempFilePath, CancellationToken.None);
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke lagre dokumentversjonen. Prøv på nytt."));
        }

        // DB er nå persistent — flytt filer
        if (!string.IsNullOrEmpty(oldFilePath) && oldFilePath != newFilePath)
        {
            string archivedPath = $"{storageFolder}/archived/{documentId}/file_v{newVersion - 1}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(oldFileName ?? fileName)}";
            await documentFileService.MoveAsync(oldFilePath, archivedPath, cancellationToken);

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
            await documentRepository.SaveChangesAsync(cancellationToken);
        }

        // Flytt temp-fil til endelig lokasjon
        await documentFileService.MoveAsync(tempFilePath, newFilePath, cancellationToken);

        Document? updated = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken);

        if (updated is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{document.Id}' ble ikke funnet etter opplasting."));

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

        var dtos = new List<DocumentListDto>();
        foreach (Document doc in documents)
        {
            int signatureCount = allSignatures.Count(s => s.DocumentId == doc.Id && s.SignatureVersion == doc.Version);
            // signertByCurrentUser er alltid true her — dette er "mine signerte dokumenter"
            dtos.Add(DocumentMapper.ToListDto(doc, signatureCount, signedByCurrentUser: true));
        }

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
            userId, user.DepartmentId, user.JobTitle, signedDocumentIds, cancellationToken);

        if (pendingDocuments is null || pendingDocuments.Count == 0)
            return Result<IReadOnlyList<DocumentListDto>>.Success(Array.Empty<DocumentListDto>());

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(
            pendingDocuments.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        var dtos = new List<DocumentListDto>();
        foreach (Document doc in pendingDocuments)
        {
            int signatureCount = allSignatures.Count(s => s.DocumentId == doc.Id && s.SignatureVersion == doc.Version);
            // signedByCurrentUser er alltid false her — brukeren har ikke signert disse dokumentene ennå
            dtos.Add(DocumentMapper.ToListDto(doc, signatureCount, signedByCurrentUser: false));
        }

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
    }

    /// <summary>
    /// Aplikerer oppdateringer fra DTO på dokumententiteten.
    /// </summary>
    private static void ApplyUpdate(Document document, UpdateDocumentRequest request)
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

        if (request.ClearTargetDepartment)
            document.TargetDepartmentId = null;
        else if (request.TargetDepartmentId.HasValue)
            document.TargetDepartmentId = request.TargetDepartmentId.Value;

        if (request.ClearTargetJobTitle)
            document.TargetJobTitle = null;
        else if (request.TargetJobTitle is not null)
            document.TargetJobTitle = request.TargetJobTitle;
    }

    /// <summary>
    /// Validerer at target-feltene er konsistente med dokumenttypens TargetMode.
    /// Ved opprettelse (isCreate=true) kreves at påkrevde felt er satt.
    /// Ved oppdatering (isCreate=false) sjekkes kun at regler ikke brytes.
    /// </summary>
    private static Result ValidateTarget(
        DocumentType documentType,
        Guid? targetDepartmentId,
        string? targetJobTitle,
        bool isCreate)
    {
        bool hasDepartment = targetDepartmentId.HasValue;
        bool hasJobTitle = !string.IsNullOrEmpty(targetJobTitle);

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
                    $"Dokumenttype '{documentType.Name}' krever at TargetDepartmentId er satt når TargetJobTitle brukes.")),
            DocumentTargetMode.JobTitle when isCreate && !hasJobTitle =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetJobTitle er satt.")),
            _ => Result.Success()
        };
    }
}