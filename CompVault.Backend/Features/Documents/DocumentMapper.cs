using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.DTOs.Documents;

namespace CompVault.Backend.Features.Documents;

/// <summary>
/// Statisk mapper mellom dokument-entiteter og DTOs.
/// </summary>
public static class DocumentMapper
{
    public static DocumentDto ToDto(Document document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            DocumentTypeId = document.DocumentTypeId,
            DocumentTypeSlug = document.DocumentType?.Slug ?? string.Empty,
            DocumentTypeCategoryId = document.DocumentTypeCategoryId,
            CategoryName = document.Category?.Name,
            Title = document.Title,
            Description = document.Description,
            ExternalUrl = document.ExternalUrl,
            TargetDepartmentIds = document.DocumentDepartments.Select(dd => dd.DepartmentId).ToList(),
            TargetDepartmentNames = document.DocumentDepartments
                .Where(dd => dd.Department != null)
                .Select(dd => dd.Department!.Name)
                .ToList(),
            TargetJobTitleIds = document.DocumentJobTitles.Select(dj => dj.JobTitleId).ToList(),
            TargetJobTitleNames = document.DocumentJobTitles
                .Where(dj => dj.JobTitle != null)
                .Select(dj => dj.JobTitle!.Name)
                .ToList(),
            RequiresSignature = document.RequiresSignature,
            HasFile = !string.IsNullOrEmpty(document.FilePath),
            Version = document.Version,
            FileName = document.FileName,
            FileSize = document.FileSize,
            MimeType = document.MimeType,
            UploadedBy = document.UploadedBy,
            UploadedByName = document.Uploader is { } uploader
                ? $"{uploader.FirstName} {uploader.LastName}".Trim()
                : null,
            UploadedAt = document.UploadedAt
        };
    }

    public static DocumentListDto ToListDto(
        Document document, int totalSignatures, bool signedByCurrentUser)
    {
        return new DocumentListDto
        {
            Id = document.Id,
            Slug = document.DocumentType?.Slug ?? string.Empty,
            Title = document.Title,
            Description = document.Description,
            DocumentTypeCategoryId = document.DocumentTypeCategoryId,
            CategoryName = document.Category?.Name,
            ExternalUrl = document.ExternalUrl,
            HasFile = !string.IsNullOrEmpty(document.FilePath),
            FileName = document.FileName,
            TargetDepartmentIds = document.DocumentDepartments.Select(dd => dd.DepartmentId).ToList(),
            TargetDepartmentNames = document.DocumentDepartments
                .Where(dd => dd.Department != null)
                .Select(dd => dd.Department!.Name)
                .ToList(),
            TargetJobTitleIds = document.DocumentJobTitles.Select(dj => dj.JobTitleId).ToList(),
            TargetJobTitleNames = document.DocumentJobTitles
                .Where(dj => dj.JobTitle != null)
                .Select(dj => dj.JobTitle!.Name)
                .ToList(),
            RequiresSignature = document.RequiresSignature,
            Version = document.Version,
            UploadedByName = document.Uploader is { } uploader
                ? $"{uploader.FirstName} {uploader.LastName}".Trim()
                : null,
            UploadedAt = document.UploadedAt,
            TotalSignatures = totalSignatures,
            SignedByCurrentUser = signedByCurrentUser
        };
    }

    public static UserSignatureStatusDto ToSignatureStatusDto(ApplicationUser user, DocumentSignature? signature)
    {
        return new UserSignatureStatusDto
        {
            UserId = user.Id,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            JobTitleId = user.JobTitleId,
            JobTitleName = user.JobTitle?.Name,
            DepartmentId = user.DepartmentId,
            DepartmentName = user.Department?.Name,
            HasSigned = signature is not null,
            SignedAt = signature?.SignedAt,
            SignatureVersion = signature?.SignatureVersion
        };
    }

    public static DocumentTypeDto ToTypeDto(DocumentType documentType)
    {
        return new DocumentTypeDto
        {
            Id = documentType.Id,
            Name = documentType.Name,
            Slug = documentType.Slug,
            Description = documentType.Description,
            TargetMode = documentType.TargetMode,
            AllowedMimeTypes = documentType.AllowedMimeTypes,
            MaxFileSizeBytes = documentType.MaxFileSizeBytes,
            IsActive = documentType.IsActive,
            CreatedAt = documentType.CreatedAt,
            CategoryCount = documentType.Categories?.Count ?? 0
        };
    }

    public static DocumentTypeCategoryDto ToCategoryDto(DocumentTypeCategory category)
    {
        return new DocumentTypeCategoryDto
        {
            Id = category.Id,
            DocumentTypeId = category.DocumentTypeId,
            Name = category.Name,
            Slug = category.Slug,
            IsActive = category.IsActive
        };
    }

    /// <summary>
    /// Kartlegger en liste dokumenter til DocumentListDto med signaturstatistikk.
    /// Centraliserer logikken for å telle signaturer og sjekke om brukeren har signert.
    /// </summary>
    public static List<DocumentListDto> MapToListDtos(
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

            bool signedByCurrentUser = signedByCurrentUserOverride ?? (currentUserId.HasValue &&
                allSignatures.Any(s =>
                    s.DocumentId == doc.Id &&
                    s.SignatureVersion == doc.Version &&
                    s.UserId == currentUserId.Value));

            dtos.Add(ToListDto(doc, signatureCount, signedByCurrentUser));
        }

        return dtos;
    }
}