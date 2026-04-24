using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <summary>
/// Core CRUD for dokumenter — henting, oppretting, oppdatering og sletting.
/// Filversjonering og nedlasting: <see cref="IDocumentVersioningService"/>.
/// Signatur: <see cref="IDocumentSignatureService"/>.
/// Målgruppe-logikk: <see cref="IDocumentTargetingService"/>.
/// </summary>
public interface IDocumentService
{
    /// <summary>Henter alle dokumenter for en dokumenttype med filtrering.</summary>
    Task<Result<IReadOnlyList<DocumentListDto>>> GetAllAsync(
        string documentTypeSlug,
        Guid? currentUserId,
        Guid? documentTypeCategoryId,
        bool bypassTargeting = false,
        CancellationToken cancellationToken = default);

    /// <summary>Henter ett dokument basert på ID.</summary>
    Task<Result<DocumentDto>> GetByIdAsync(Guid id, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default);

    /// <summary>Henter alle dokumenter paginert for en spesifikk bruker,
    /// filtert utifra status hvis ønskelig</summary>
    Task<Result<PagedResult<DocumentListDto>>> GetDocumentsForUserAsync(
        Guid userId,
        DocumentQueryParameters query,
        bool hasPermission,
        CancellationToken ct = default);
    
    /// <summary>Oppretter et nytt dokument med valgfri filopplasting.</summary>
    Task<Result<DocumentDto>> CreateAsync(
        string documentTypeSlug,
        CreateDocumentRequest request,
        Guid uploadedById,
        bool bypassTarget,
        string? fileName = null,
        string? contentType = null,
        Stream? fileStream = null,
        CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer metadata på et dokument.</summary>
    Task<Result<DocumentDto>> UpdateAsync(
        Guid id, Guid userId, UpdateDocumentRequest request, bool bypassTarget, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter et dokument.</summary>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}