using CompVault.Backend.Domain.Entities.Documents;
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
            Version = document.Version,
            UploadedByName = document.Uploader is { } uploader
                ? $"{uploader.FirstName} {uploader.LastName}".Trim()
                : null,
            UploadedAt = document.UploadedAt,
            TotalSignatures = totalSignatures,
            SignedByCurrentUser = signedByCurrentUser
        };
    }

    public static DocumentSignatureDto ToSignatureDto(DocumentSignature signature)
    {
        return new DocumentSignatureDto
        {
            Id = signature.Id,
            DocumentId = signature.DocumentId,
            UserId = signature.UserId,
            UserName = signature.User is { } user
                ? $"{user.FirstName} {user.LastName}".Trim()
                : string.Empty,
            SignedAt = signature.SignedAt,
            SignatureVersion = signature.SignatureVersion
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
}