using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <summary>
/// Administrasjon av dokumenttyper — opprett, oppdater, slett og kategorier.
/// </summary>
public interface IDocumentTypeService
{
    /// <summary>Henter alle aktive dokumenttyper.</summary>
    Task<Result<IReadOnlyList<DocumentTypeDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter én dokumenttype basert på slug.</summary>
    Task<Result<DocumentTypeDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Oppretter en ny dokumenttype.</summary>
    Task<Result<DocumentTypeDto>> CreateAsync(
        CreateDocumentTypeRequest request, Guid createdById, CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer en dokumenttype.</summary>
    Task<Result<DocumentTypeDto>> UpdateAsync(
        string slug, UpdateDocumentTypeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter en dokumenttype.</summary>
    Task<Result<bool>> DeleteAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Henter kategorier for en dokumenttype.</summary>
    Task<Result<IReadOnlyList<DocumentTypeCategoryDto>>> GetCategoriesAsync(
        string documentTypeSlug, CancellationToken cancellationToken = default);

    /// <summary>Oppretter en ny kategori under en dokumenttype.</summary>
    Task<Result<DocumentTypeCategoryDto>> CreateCategoryAsync(
        string documentTypeSlug, CreateDocumentTypeCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer en kategori.</summary>
    Task<Result<DocumentTypeCategoryDto>> UpdateCategoryAsync(
        string documentTypeSlug, Guid categoryId, UpdateDocumentTypeCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sletter en kategori.</summary>
    Task<Result<bool>> DeleteCategoryAsync(
        string documentTypeSlug, Guid categoryId, CancellationToken cancellationToken = default);
}