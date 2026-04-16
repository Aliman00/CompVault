using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Documents.Services;

/// <inheritdoc />
public sealed class DocumentTypeService(
    IDocumentTypeRepository documentTypeRepository,
    IDocumentTypeCategoryRepository categoryRepository) : IDocumentTypeService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentTypeDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentType> types = await documentTypeRepository.GetAllWithCategoriesAsync(cancellationToken);
        var dtos = types.Select(DocumentMapper.ToTypeDto).ToList();
        return Result<IReadOnlyList<DocumentTypeDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeDto>> GetBySlugAsync(
        string slug, CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetWithCategoriesBySlugAsync(slug, cancellationToken);

        if (documentType is null)
            return Result<DocumentTypeDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{slug}' ble ikke funnet."));

        return Result<DocumentTypeDto>.Success(DocumentMapper.ToTypeDto(documentType));
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeDto>> CreateAsync(
        CreateDocumentTypeRequest request, Guid createdById, CancellationToken cancellationToken = default)
    {
        // Sjekk at slug er unik
        bool slugExists = await documentTypeRepository.SlugExistsAsync(request.Slug, cancellationToken: cancellationToken);
        if (slugExists)
            return Result<DocumentTypeDto>.Failure(
                AppError.Conflict($"Slug '{request.Slug}' er allerede i bruk."));

        var documentType = new DocumentType
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            TargetMode = request.TargetMode,
            StorageFolder = request.Slug,
            AllowedMimeTypes = request.AllowedMimeTypes,
            MaxFileSizeBytes = request.MaxFileSizeBytes,
            CreatedById = createdById
        };

        await documentTypeRepository.AddAsync(documentType, cancellationToken);

        try
        {
            await documentTypeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Slug-unikhet sjekkes før lagring, men concurrent requests kan likevel
            // opprette samme slug. Unique constraint fanger dette opp.
            return Result<DocumentTypeDto>.Failure(
                AppError.Conflict($"Dokumenttype med slug '{request.Slug}' kunne ikke opprettes. Prøv på nytt."));
        }

        DocumentType? created = await documentTypeRepository.GetWithCategoriesAsync(documentType.Id, cancellationToken);
        if (created is null)
            return Result<DocumentTypeDto>.Failure(
                AppError.NotFound($"Dokumenttype med ID '{documentType.Id}' ble ikke funnet etter opprettelse."));

        return Result<DocumentTypeDto>.Success(DocumentMapper.ToTypeDto(created));
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeDto>> UpdateAsync(
        string slug, UpdateDocumentTypeRequest request, CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(slug, cancellationToken);

        if (documentType is null)
            return Result<DocumentTypeDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{slug}' ble ikke funnet."));

        if (!string.IsNullOrEmpty(request.Name))
            documentType.Name = request.Name;

        if (request.ClearDescription)
            documentType.Description = null;
        else if (request.Description is not null)
            documentType.Description = request.Description;

        if (request.TargetMode.HasValue)
            documentType.TargetMode = request.TargetMode.Value;

        if (request.AllowedMimeTypes is not null)
            documentType.AllowedMimeTypes = request.AllowedMimeTypes;

        if (request.MaxFileSizeBytes.HasValue)
            documentType.MaxFileSizeBytes = request.MaxFileSizeBytes.Value;

        await documentTypeRepository.UpdateAsync(documentType, cancellationToken);

        try
        {
            await documentTypeRepository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent oppdatering av slug eller annen unique constraint kan trigge dette
            return Result<DocumentTypeDto>.Failure(
                AppError.Conflict($"Dokumenttype med slug '{slug}' kunne ikke oppdateres. Prøv på nytt."));
        }

        DocumentType? updated = await documentTypeRepository.GetWithCategoriesAsync(documentType.Id, cancellationToken);
        if (updated is null)
            return Result<DocumentTypeDto>.Failure(
                AppError.NotFound($"Dokumenttype med ID '{documentType.Id}' ble ikke funnet etter oppdatering."));

        return Result<DocumentTypeDto>.Success(DocumentMapper.ToTypeDto(updated));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(string slug, CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(slug, cancellationToken);

        if (documentType is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{slug}' ble ikke funnet."));

        documentType.IsActive = false;
        documentType.DeletedAt = DateTime.UtcNow;

        await documentTypeRepository.UpdateAsync(documentType, cancellationToken);
        await documentTypeRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DocumentTypeCategoryDto>>> GetCategoriesAsync(
        string documentTypeSlug, CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<IReadOnlyList<DocumentTypeCategoryDto>>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        IReadOnlyList<DocumentTypeCategory> categories =
            await categoryRepository.GetByDocumentTypeIdAsync(documentType.Id, cancellationToken);

        var dtos = categories.Select(DocumentMapper.ToCategoryDto).ToList();
        return Result<IReadOnlyList<DocumentTypeCategoryDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeCategoryDto>> CreateCategoryAsync(
        string documentTypeSlug, CreateDocumentTypeCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<DocumentTypeCategoryDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        bool slugExists = await categoryRepository.SlugExistsAsync(
            documentType.Id, request.Slug, cancellationToken: cancellationToken);

        if (slugExists)
            return Result<DocumentTypeCategoryDto>.Failure(
                AppError.Conflict($"Kategori-slug '{request.Slug}' finnes allerede for denne dokumenttypen."));

        var category = new DocumentTypeCategory
        {
            DocumentTypeId = documentType.Id,
            Name = request.Name,
            Slug = request.Slug
        };

        await categoryRepository.AddAsync(category, cancellationToken);
        await categoryRepository.SaveChangesAsync(cancellationToken);

        return Result<DocumentTypeCategoryDto>.Success(DocumentMapper.ToCategoryDto(category));
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeCategoryDto>> UpdateCategoryAsync(
        string documentTypeSlug, Guid categoryId, UpdateDocumentTypeCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<DocumentTypeCategoryDto>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        DocumentTypeCategory? category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

        if (category is null || category.DocumentTypeId != documentType.Id)
            return Result<DocumentTypeCategoryDto>.Failure(
                AppError.NotFound($"Kategori med ID '{categoryId}' ble ikke funnet for dokumenttype '{documentTypeSlug}'."));

        bool slugExists = await categoryRepository.SlugExistsAsync(
            documentType.Id, request.Slug, excludeId: categoryId, cancellationToken: cancellationToken);

        if (slugExists)
            return Result<DocumentTypeCategoryDto>.Failure(
                AppError.Conflict($"Kategori-slug '{request.Slug}' finnes allerede for denne dokumenttypen."));

        category.Name = request.Name;
        category.Slug = request.Slug;

        await categoryRepository.UpdateAsync(category, cancellationToken);
        await categoryRepository.SaveChangesAsync(cancellationToken);

        return Result<DocumentTypeCategoryDto>.Success(DocumentMapper.ToCategoryDto(category));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteCategoryAsync(
        string documentTypeSlug, Guid categoryId, CancellationToken cancellationToken = default)
    {
        DocumentType? documentType = await documentTypeRepository.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (documentType is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Dokumenttype med slug '{documentTypeSlug}' ble ikke funnet."));

        DocumentTypeCategory? category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

        if (category is null || category.DocumentTypeId != documentType.Id)
            return Result<bool>.Failure(
                AppError.NotFound($"Kategori med ID '{categoryId}' ble ikke funnet for dokumenttype '{documentTypeSlug}'."));

        category.IsActive = false;
        category.DeletedAt = DateTime.UtcNow;
        await categoryRepository.UpdateAsync(category, cancellationToken);
        await categoryRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}