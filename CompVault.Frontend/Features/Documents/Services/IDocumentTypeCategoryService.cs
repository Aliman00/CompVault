using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Documents.Services;

public interface IDocumentTypeCategoryService
{
    /// <summary>
    /// Henter alle kategorier for en dokumenttype
    /// </summary>
    Task<Result<List<DocumentTypeCategoryDto>>> GetAllAsync(string documentTypeSlug, CancellationToken ct);

    /// <summary>
    /// Oppretter en ny kategori under en dokumenttype
    /// </summary>
    Task<Result<DocumentTypeCategoryDto>> CreateAsync(string documentTypeSlug,
        CreateDocumentTypeCategoryRequest request, CancellationToken ct);

    /// <summary>
    /// Oppdaterer en eksisterende kategori
    /// </summary>
    Task<Result<DocumentTypeCategoryDto>> UpdateAsync(string documentTypeSlug, Guid categoryId,
        UpdateDocumentTypeCategoryRequest request, CancellationToken ct);

    /// <summary>
    /// Soft-deleter en kategori
    /// </summary>
    Task<Result> DeleteAsync(string documentTypeSlug, Guid categoryId, CancellationToken ct);
}