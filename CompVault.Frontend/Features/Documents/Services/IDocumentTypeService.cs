using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public interface IDocumentTypeService
{
    /// <summary>
    /// Henter alle dokumenttyper fra backend
    /// </summary>
    Task<Result<List<DocumentTypeDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Henter en dokumenttype basert på slug
    /// </summary>
    Task<Result<DocumentTypeDto>> GetBySlugAsync(string slug, CancellationToken ct);

    /// <summary>
    /// Oppretter em ny dokumenttype
    /// </summary>
    Task<Result<DocumentTypeDto>> CreateAsync(CreateDocumentTypeRequest request, CancellationToken ct);

    /// <summary>
    /// Oppdaterer en eksisterende dokumenttype
    /// </summary>
    Task<Result<DocumentTypeDto>> UpdateAsync(string slug, UpdateDocumentTypeRequest request, CancellationToken ct);

    /// <summary>
    /// Soft-deleter en dokumenttype
    /// </summary>
    Task<Result> DeleteAsync(string slug, CancellationToken ct);
}