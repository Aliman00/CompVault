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

        // Valider kategori tilhører riktig dokumenttype og er aktiv
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

        // Hvis fil er lastet opp, lagre den
        if (fileStream is not null && fileName is not null && contentType is not null)
        {
            // Valider MIME-type mot dokumenttypens tillatte typer
            if (!documentType.AllowedMimeTypes.Contains(contentType))
                return Result<DocumentDto>.Failure(
                    AppError.Create(ErrorCode.Validation,
                        $"Filtypen '{contentType}' er ikke tillatt for denne dokumenttypen."));

            // Valider filstørrelse mot dokumenttypens grense
            if (documentType.MaxFileSizeBytes > 0 && fileStream.Length > documentType.MaxFileSizeBytes)
                return Result<DocumentDto>.Failure(
                    AppError.Create(ErrorCode.Validation,
                        $"Filen er for stor. Maks tillatt størrelse: {documentType.MaxFileSizeBytes / (1024 * 1024)}MB."));

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

        try
        {
            await documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError,
                    "Kunne ikke lagre dokumentet. Prøv på nytt."));
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

        // Hent dokumenttype for å validere TargetMode og kategori-eierskap
        DocumentType? documentType = document.DocumentType;
        if (documentType is null)
        {
            DocumentType? fetched = await documentTypeRepository.GetByIdAsync(document.DocumentTypeId, cancellationToken);
            if (fetched is null)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Dokumenttype for dokumentet ble ikke funnet."));
            documentType = fetched;
        }

        // Valider TargetMode hvis target-felter forsøkes endret
        bool wantsTargetDepartment = request.TargetDepartmentId.HasValue && !request.ClearTargetDepartment;
        bool wantsTargetJobTitle = !string.IsNullOrEmpty(request.TargetJobTitle) && !request.ClearTargetJobTitle;

        if (wantsTargetDepartment || wantsTargetJobTitle)
        {
            Result targetValidation = ValidateTargetForUpdate(documentType, wantsTargetDepartment, request.TargetDepartmentId, wantsTargetJobTitle);
            if (targetValidation.IsFailure)
                return Result<DocumentDto>.Failure(targetValidation.Error!);
        }

        // Valider at target-department eksisterer hvis det settes
        if (request.TargetDepartmentId.HasValue && !request.ClearTargetDepartment)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.TargetDepartmentId.Value, cancellationToken);

            if (!departmentExists)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.TargetDepartmentId.Value}' ble ikke funnet."));
        }

        // Valider kategori tilhører dokumenttypen og er aktiv hvis den settes
        if (request.DocumentTypeCategoryId.HasValue && !request.ClearDocumentTypeCategoryId)
        {
            DocumentType? documentTypeWithCategories = await documentTypeRepository.GetWithCategoriesBySlugAsync(documentType.Slug, cancellationToken);
            if (documentTypeWithCategories is null || documentTypeWithCategories.Categories.All(c => c.Id != request.DocumentTypeCategoryId.Value))
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Kategori med ID '{request.DocumentTypeCategoryId.Value}' finnes ikke for dokumentets dokumenttype."));
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

        try
        {
            await documentRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.InternalError,
                    "Kunne ikke lagre dokumentendringene. Prøv på nytt."));
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

        // RequiresSignature == false betyr at signering ikke er nødvendig
        if (!document.RequiresSignature)
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Dette dokumentet krever ikke signering."));

        // Sjekk at bruker tilhører dokumentets målgruppe
        if (document.TargetDepartmentId.HasValue || !string.IsNullOrEmpty(document.TargetJobTitle))
        {
            ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result<bool>.Failure(
                    AppError.NotFound("Bruker ble ikke funnet."));

            bool inTargetGroup =
                (document.TargetDepartmentId is null || document.TargetDepartmentId == user.DepartmentId) &&
                (string.IsNullOrEmpty(document.TargetJobTitle) || document.TargetJobTitle == user.JobTitle);

            if (!inTargetGroup)
                return Result<bool>.Failure(
                    AppError.Create(ErrorCode.Forbidden,
                        "Du tilhører ikke målgruppen for dette dokumentet."));
        }

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
                AppError.Create(ErrorCode.Conflict,
                    "Du har allerede signert denne versjonen av dokumentet."));
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
        // Hent dokumenttype fra slug og bekreft at dokumentet tilhører denne typen
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        Document? document = await documentRepository.GetForUpdateAsync(documentId, cancellationToken);

        if (document is null)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' ble ikke funnet."));

        // Bekreft at dokumentet faktisk tilhører slugens dokumenttype
        if (document.DocumentTypeId != documentType.Id)
            return Result<DocumentDto>.Failure(
                AppError.NotFound($"Dokument med ID '{documentId}' er ikke av dokumenttype '{documentTypeSlug}'."));

        // Valider at filtypen er tillatt for denne dokumenttypen
        if (!documentType.AllowedMimeTypes.Contains(contentType))
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Filtypen '{contentType}' er ikke tillatt for denne dokumenttypen."));

        // Valider at filstørrelse ikke overskrider dokumenttypens grense
        long fileSize = stream.Length;
        if (documentType.MaxFileSizeBytes > 0 && fileSize > documentType.MaxFileSizeBytes)
            return Result<DocumentDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Filen er for stor. Maks tillatt størrelse: {documentType.MaxFileSizeBytes / (1024 * 1024)}MB."));

        string extension = Path.GetExtension(fileName);
        string storageFolder = documentType.StorageFolder;
        string tempPath = $"{storageFolder}/active/{documentId}/file_v{document.Version + 1}_tmp{extension}";

        // Lagre stream-lengden før kopiering — etter CopyToAsync kan positionen være endret
        stream.Position = 0;

        try
        {
            // Lagre til midlertidig fil for sjekksum-beregning
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
            string newFilePath = $"{storageFolder}/active/{documentId}/file_v{newVersion}{extension}";

            // Arkivér eksisterende fil OG dens metadata FØR vi overskriver document.FilePath.
            // Vi trenger begge verdiene etter at document.* er endret.
            string? oldFilePath = document.FilePath;
            string? oldFileName = document.FileName;
            string? oldMimeType = document.MimeType;
            long? oldFileSize = document.FileSize;
            string? oldChecksum = document.Checksum;

            // Oppdater dokument metadata først — database-endringen blir persistent
            // ved SaveChangesAsync. Først når det lykkes flytter vi filer.
            document.Version = newVersion;
            document.FileName = fileName;
            document.FilePath = newFilePath;
            document.FileSize = fileSize;
            document.MimeType = contentType;
            document.Checksum = newChecksum;
            document.UploadedBy = uploadedById;
            document.UploadedAt = DateTime.UtcNow;

            // Slett signaturer — ny versjon krever re-signering
            await signatureRepository.DeleteAllForDocumentAsync(documentId, cancellationToken);

            // Persistér DB-endringer FØR filer flyttes.
            // Hvis SaveChanges feiler er ingen filer berørt og operasjonen kan trygt gjentas.
            await documentRepository.SaveChangesAsync(cancellationToken);

            // Nå som DB er persistent — flytt filer.
            // Ved feil her er DB konsistent, men filer kan være i en ufullstendig tilstand.
            // Det er akseptabelt — filene kan ryddes manuelt, og ingen data går tapt.
            if (!string.IsNullOrEmpty(oldFilePath) && oldFilePath != newFilePath)
            {
                string archivedPath = $"{storageFolder}/archived/{documentId}/file_v{newVersion - 1}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(oldFileName ?? fileName)}";
                await fileStorage.MoveAsync(oldFilePath, archivedPath, cancellationToken);

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

            // Flytt temp-fil til endelig aktiv lokasjon
            await fileStorage.MoveAsync(tempPath, newFilePath, cancellationToken);

            Document? updated = await documentRepository.GetWithDetailsAsync(document.Id, cancellationToken);

            if (updated is null)
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Dokument med ID '{document.Id}' ble ikke funnet etter opplasting."));

            return Result<DocumentDto>.Success(DocumentMapper.ToDto(updated));
        }
        catch
        {
            // Rydd opp i midlertidig fil ved alle typer feil
            await fileStorage.DeleteAsync(tempPath, CancellationToken.None);
            throw;
        }
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
        => fileStorage.OpenReadAsync(relativePath, cancellationToken);

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

        // Hent alle pending dokumenter i én spørring
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
            dtos.Add(DocumentMapper.ToListDto(doc, signatureCount, signedByCurrentUser: false));
        }

        return Result<IReadOnlyList<DocumentListDto>>.Success(dtos);
    }

    /// <summary>
    /// Validerer at target-feltene stemmer med dokumenttypens TargetMode ved oppdatering.
    /// </summary>
    private static Result ValidateTargetForUpdate(
        DocumentType documentType,
        bool wantsTargetDepartment, Guid? targetDepartmentId,
        bool wantsTargetJobTitle)
    {
        return documentType.TargetMode switch
        {
            DocumentTargetMode.None when wantsTargetDepartment || wantsTargetJobTitle =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' har TargetMode=None. Target-felt kan ikke settes.")),
            DocumentTargetMode.Department when wantsTargetJobTitle && !wantsTargetDepartment && targetDepartmentId is null =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetDepartmentId er satt når TargetJobTitle brukes.")),
            _ => Result.Success()
        };
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