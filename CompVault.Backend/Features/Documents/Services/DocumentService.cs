using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.FileStorage;
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
    IFileStorageService fileStorage) : IDocumentService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentListDto>>> GetAllAsync(
        string documentTypeSlug,
        Guid? currentUserId,
        Guid? documentTypeCategoryId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<IReadOnlyList<DocumentListDto>>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        IReadOnlyList<Document> documents = await documentRepository.GetByDocumentTypeAsync(
            documentType.Id, documentTypeCategoryId, includeArchived, cancellationToken);

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

            bool signedByCurrentUser = currentUserId.HasValue && doc.IsCurrent && allSignatures.Any(s =>
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
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        // Valider target basert på dokumenttypens TargetMode
        Result targetValidation = ValidateTarget(documentType, request.TargetDepartmentId, request.TargetJobTitle);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(
                targetValidation.Error ?? throw new InvalidOperationException("Target-validering feilet uten feilmelding."));

        // Valider avdeling eksisterer hvis TargetDepartmentId er satt
        if (request.TargetDepartmentId.HasValue)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.TargetDepartmentId.Value, cancellationToken);

            if (!departmentExists)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.TargetDepartmentId.Value}' ble ikke funnet."));
        }

        // Valider kategori tilhører riktig dokumenttype
        if (request.DocumentTypeCategoryId.HasValue)
        {
            DocumentType? documentTypeWithCategories = await documentTypeRepository.GetWithCategoriesAsync(documentType.Id, cancellationToken);
            if (documentTypeWithCategories is null || documentTypeWithCategories.Categories.All(c => c.Id != request.DocumentTypeCategoryId.Value))
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Kategori med ID '{request.DocumentTypeCategoryId.Value}' finnes ikke for denne dokumenttypen."));
        }

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
            IsCurrent = true,
            UploadedBy = uploadedById,
            IsActive = true
        };

        // Hvis fil er lastet opp, lagre den
        if (fileStream is not null && fileName is not null && contentType is not null)
        {
            string extension = Path.GetExtension(fileName);
            string newFilePath = $"{documentType.StorageFolder}/active/{document.Id}/file_v1{extension}";

            if (fileStream.Position != 0)
                fileStream.Position = 0;

            await fileStorage.SaveAsync(fileStream, newFilePath, cancellationToken);
            string checksum = await fileStorage.ComputeChecksumAsync(newFilePath, cancellationToken);

            document.FileName = fileName;
            document.FilePath = newFilePath;
            document.FileSize = fileStream.Length;
            document.MimeType = contentType;
            document.Checksum = checksum;
        }

        await documentRepository.AddAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);

        Document created = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Dokument med ID '{document.Id}' ble ikke funnet etter opprettelse.");

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

        // Valider avdeling hvis endret
        if (request.TargetDepartmentId.HasValue && !request.ClearTargetDepartment)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.TargetDepartmentId.Value, cancellationToken);

            if (!departmentExists)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.TargetDepartmentId.Value}' ble ikke funnet."));
        }

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

        await documentRepository.SaveChangesAsync(cancellationToken);

        Document updated = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Dokument med ID '{document.Id}' ble ikke funnet etter oppdatering.");

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

        if (!document.IsCurrent)
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Kun gjeldende versjon av et dokument kan signeres."));

        // null/true = signering tillatt, false = dokumentet krever ikke signering
        if (document.RequiresSignature == false)
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Dette dokumentet krever ikke signering."));

        bool alreadySigned = await signatureRepository.HasUserSignedVersionAsync(
            documentId, userId, document.Version, cancellationToken);

        if (alreadySigned)
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Conflict,
                    "Du har allerede signert denne versjonen av dokumentet."));

        var signature = new DocumentSignature
        {
            DocumentId = documentId,
            UserId = userId,
            SignatureVersion = document.Version
        };

        await signatureRepository.AddAsync(signature, cancellationToken);
        await signatureRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> UploadVersionAsync(
        Guid documentId,
        string fileName,
        string contentType,
        Stream stream,
        Guid uploadedById,
        CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetForUpdateAsync(documentId, cancellationToken);

        if (document is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        string extension = Path.GetExtension(fileName);
        string storageFolder = document.DocumentType?.StorageFolder ?? DocumentConstants.DefaultStorageFolder;
        string tempPath = $"{storageFolder}/active/{documentId}/file_v{document.Version + 1}_tmp{extension}";

        // Lagre til midlertidig fil for sjekksum-beregning
        stream.Position = 0;
        await fileStorage.SaveAsync(stream, tempPath, cancellationToken);

        string newChecksum = await fileStorage.ComputeChecksumAsync(tempPath, cancellationToken);

        // Sjekk om filen er identisk med forrige versjon
        if (document.Checksum is not null && document.Checksum == newChecksum)
        {
            await fileStorage.DeleteAsync(tempPath, cancellationToken);
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Filinnholdet er identisk med forrige versjon. Ingen ny versjon opprettet."));
        }

        int newVersion = document.Version + 1;

        // Arkiver gammel fil hvis den eksisterer
        if (!string.IsNullOrEmpty(document.FilePath))
        {
            string archivedPath = $"{storageFolder}/archived/{documentId}/file_v{document.Version}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(document.FileName)}";
            await fileStorage.MoveAsync(document.FilePath, archivedPath, cancellationToken);

            var versionRecord = new DocumentVersion
            {
                DocumentId = documentId,
                Version = document.Version,
                FileName = document.FileName,
                FilePath = archivedPath,
                FileSize = document.FileSize,
                MimeType = document.MimeType,
                Checksum = document.Checksum,
                ArchivedAt = DateTime.UtcNow
            };

            await documentRepository.AddVersionAsync(versionRecord, cancellationToken);
        }

        // Flytt midlertidig fil til endelig plassering
        string newFilePath = $"{storageFolder}/active/{documentId}/file_v{newVersion}{extension}";
        await fileStorage.MoveAsync(tempPath, newFilePath, cancellationToken);

        // Oppdater dokument
        document.Version = newVersion;
        document.FileName = fileName;
        document.FilePath = newFilePath;
        document.FileSize = stream.Length;
        document.MimeType = contentType;
        document.Checksum = newChecksum;
        document.UploadedBy = uploadedById;
        document.UploadedAt = DateTime.UtcNow;
        document.IsCurrent = true;

        // Slett signaturer — ny versjon krever re-signering
        await signatureRepository.DeleteAllForDocumentAsync(documentId, cancellationToken);

        await documentRepository.SaveChangesAsync(cancellationToken);

        Document updated = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Dokument med ID '{document.Id}' ble ikke funnet etter opplasting.");

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
                AppError.Create(ErrorCode.Validation,
                    "Dokumentet har ingen filvedlegg. Kun ekstern lenke er tilgjengelig."));

        bool fileExists = await fileStorage.ExistsAsync(document.FilePath, cancellationToken);

        if (!fileExists)
            return Result<DocumentDownloadResult>.Failure(
                AppError.NotFound($"Filen for dokument med ID '{documentId}' ble ikke funnet på lagring."));

        Stream fileStream = await fileStorage.OpenReadAsync(document.FilePath, cancellationToken);

        var result = new DocumentDownloadResult
        {
            Stream = fileStream,
            FileName = document.FileName ?? "dokument.pdf",
            ContentType = document.MimeType ?? "application/octet-stream",
            FileSize = document.FileSize
        };

        return Result<DocumentDownloadResult>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentSignatureDto>>> GetSignaturesAsync(
        Guid documentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        Document? document = await documentRepository.GetByIdAsync(documentId, cancellationToken);

        if (document is null)
            return Result<IReadOnlyList<DocumentSignatureDto>>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        IReadOnlyList<DocumentSignature> signatures = await signatureRepository.GetForDocumentVersionAsync(
            documentId, document.Version, cancellationToken);

        // Alle med lesetilgang ser alle signaturer
        // Fremtidig: filtrer basert på rolle/avdeling om ønskelig
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
            .Where(d => d.IsCurrent && d.IsActive)
            .ToList();

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(documents.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        var dtos = new List<DocumentListDto>();
        foreach (Document doc in documents)
        {
            int signatureCount = allSignatures.Count(s => s.DocumentId == doc.Id && s.SignatureVersion == doc.Version);
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

        // Hent alle aktive dokumenttyper
        IReadOnlyList<DocumentType> documentTypes = await documentTypeRepository.GetAllAsync(cancellationToken);
        var pendingDocuments = new List<Document>();

        foreach (DocumentType documentType in documentTypes.Where(dt => dt.IsActive))
        {
            IReadOnlyList<Document> docsForType = documentType.TargetMode switch
            {
                DocumentTargetMode.Department when user.DepartmentId.HasValue =>
                    await documentRepository.GetActiveCurrentForDepartmentAsync(
                        user.DepartmentId.Value, documentType.Id, cancellationToken),
                DocumentTargetMode.JobTitle when !string.IsNullOrEmpty(user.JobTitle) =>
                    await documentRepository.GetActiveCurrentForJobTitleAsync(
                        user.JobTitle, documentType.Id, cancellationToken),
                DocumentTargetMode.None =>
                    await documentRepository.GetAllActiveCurrentAsync(
                        documentType.Id, cancellationToken),
                _ => Array.Empty<Document>()
            };

            // Filtrer bort dokumenter brukeren allerede har signert
            pendingDocuments.AddRange(docsForType.Where(d => !signedDocumentIds.Contains(d.Id)));
        }

        if (pendingDocuments.Count == 0)
            return Result<IReadOnlyList<DocumentListDto>>.Success(Array.Empty<DocumentListDto>());

        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(pendingDocuments.Select(d => d.Id).ToList(), cancellationToken)).ToList();

        var dtos = new List<DocumentListDto>();
        foreach (Document doc in pendingDocuments)
        {
            int signatureCount = allSignatures.Count(s => s.DocumentId == doc.Id && s.SignatureVersion == doc.Version);
            dtos.Add(DocumentMapper.ToListDto(doc, signatureCount, signedByCurrentUser: false));
        }

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
    }

    /// <summary>
    /// Validerer at target-feltene stemmer med dokumenttypens TargetMode.
    /// </summary>
    private static Result ValidateTarget(
        DocumentType documentType, Guid? targetDepartmentId, string? targetJobTitle)
    {
        return documentType.TargetMode switch
        {
            DocumentTargetMode.None when targetDepartmentId.HasValue || !string.IsNullOrEmpty(targetJobTitle) =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' har TargetMode=None. Target-felt kan ikke settes.")),
            DocumentTargetMode.Department when !targetDepartmentId.HasValue =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetDepartmentId er satt.")),
            DocumentTargetMode.JobTitle when string.IsNullOrEmpty(targetJobTitle) =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetJobTitle er satt.")),
            _ => Result.Success()
        };
    }
}