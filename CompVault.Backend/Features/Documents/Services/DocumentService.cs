using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
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
    IDocumentTargetingService targetingService,
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
                .Where(d => targetingService.CanUserAccessDocument(d, userDeptId, userJobTitleId))
                .ToList();
        }

        // Batch-hent alle signaturer
        var docIds = documents.Select(d => d.Id).ToList();
        var allSignatures = (await signatureRepository.GetByDocumentIdsAsync(docIds, cancellationToken)).ToList();

        List<DocumentListDto> dtos = DocumentMapper.MapToListDtos(documents, allSignatures, currentUserId);

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

        Result accessResult = await targetingService.CheckAccessAsync(document, currentUserId, bypassTargeting, cancellationToken);
        if (accessResult.IsFailure)
            return Result<DocumentDto>.Failure(accessResult.Error!);

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

        // Valider target-lister mot dokumenttypens TargetMode
        Result targetValidation = targetingService.ValidateTarget(
            documentType, request.TargetDepartmentIds, request.TargetJobTitleIds, isCreate: true);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        // Valider at alle avdelinger finnes
        if (request.TargetDepartmentIds.Count > 0)
        {
            Result<IReadOnlyList<Domain.Entities.Departments.Department>> deptResult =
                await targetingService.GetAndValidateDepartmentsExistAsync(
                    uploadedById, request.TargetDepartmentIds, cancellationToken);
            if (deptResult.IsFailure)
                return Result<DocumentDto>.Failure(deptResult.Error!);

            // Sjekk tillatelse til avdelinger
            Result permissionResult = await targetingService.CheckDepartmentPermissionAsync(
                uploadedById, deptResult.Value!, request.TargetDepartmentIds, [], bypassTarget, cancellationToken);
            if (permissionResult.IsFailure)
                return Result<DocumentDto>.Failure(permissionResult.Error!);
        }

        // Valider at alle jobbtitler finnes
        Result jobTitleResult = await targetingService.ValidateJobTitlesExistAsync(
            request.TargetJobTitleIds, cancellationToken);
        if (jobTitleResult.IsFailure)
            return Result<DocumentDto>.Failure(jobTitleResult.Error!);

        // Valider kategori
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

        Result targetValidation = targetingService.ValidateTarget(documentType, effectiveDepartmentIds, effectiveJobTitleIds, isCreate: false);
        if (targetValidation.IsFailure)
            return Result<DocumentDto>.Failure(targetValidation.Error!);

        // Valider nye og fjernede avdelinger
        if (request.TargetDepartmentIds is not null)
        {
            var currentDepartmentIds = document.DocumentDepartments
                .Select(d => d.DepartmentId).ToHashSet();
            var addedDepartmentIds = request.TargetDepartmentIds
                .Where(d => !currentDepartmentIds.Contains(d)).ToList();
            var removedDepartmentIds = currentDepartmentIds
                .Where(d => !request.TargetDepartmentIds.Contains(d)).ToList();

            Result<IReadOnlyList<Domain.Entities.Departments.Department>> deptResult =
                await targetingService.GetAndValidateDepartmentsExistAsync(
                    userId, addedDepartmentIds, cancellationToken);
            if (deptResult.IsFailure)
                return Result<DocumentDto>.Failure(deptResult.Error!);

            Result permissionResult = await targetingService.CheckDepartmentPermissionAsync(
                userId, deptResult.Value!, addedDepartmentIds, removedDepartmentIds, bypassTarget, cancellationToken);
            if (permissionResult.IsFailure)
                return Result<DocumentDto>.Failure(permissionResult.Error!);
        }

        // Valider jobbtitler
        if (request.TargetJobTitleIds is not null && request.TargetJobTitleIds.Count > 0)
        {
            Result jobTitleResult = await targetingService.ValidateJobTitlesExistAsync(
                request.TargetJobTitleIds, cancellationToken);
            if (jobTitleResult.IsFailure)
                return Result<DocumentDto>.Failure(jobTitleResult.Error!);
        }

        // Valider kategori
        if (request.DocumentTypeCategoryId.HasValue && !request.ClearDocumentTypeCategoryId)
        {
            DocumentType? documentTypeWithCategories = await documentTypeRepository.GetWithCategoriesBySlugAsync(documentType.Slug, cancellationToken);
            if (documentTypeWithCategories is null || documentTypeWithCategories.Categories.All(c => c.Id != request.DocumentTypeCategoryId.Value))
                return Result<DocumentDto>.Failure(
                    AppError.NotFound($"Kategori med ID '{request.DocumentTypeCategoryId.Value}' finnes ikke for dokumentets dokumenttype."));
        }
        
        ApplyTargetingUpdate(document, request);
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

    /// <summary>
    /// Applierer oppdateringer fra DTO på dokumententiteten.
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
    }
    
    // Ved endring av målgruppe til en dokumenttype så kan dokumenter sitte igjen med verdier i listen til gamle
    // måltyper. Vi rydder opp i listene og legger til og fjerner gamle utifra hva brukeren legger til i requesten
    private static void ApplyTargetingUpdate(Document document, UpdateDocumentRequest request)
    {
        switch (document.DocumentType!.TargetMode)
        {
            case DocumentTargetMode.Department when request.TargetDepartmentIds is not null:
                document.DocumentDepartments.Clear();
                foreach (Guid departmentId in request.TargetDepartmentIds)
                {
                    document.DocumentDepartments.Add(
                        new DocumentDepartment { DocumentId = document.Id, DepartmentId = departmentId });
                }

                document.DocumentJobTitles.Clear();
                break;

            case DocumentTargetMode.JobTitle when request.TargetJobTitleIds is not null:
                document.DocumentJobTitles.Clear();
                foreach (Guid jobTitleId in request.TargetJobTitleIds)
                {
                    document.DocumentJobTitles.Add(
                        new DocumentJobTitle { DocumentId = document.Id, JobTitleId = jobTitleId });
                }

                document.DocumentDepartments.Clear();
                break;

            case DocumentTargetMode.None:
                document.DocumentDepartments.Clear();
                document.DocumentJobTitles.Clear();
                break;
        }
    }
}