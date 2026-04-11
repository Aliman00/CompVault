using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <summary>
/// Administrasjon av dokumenter — henting, oppretting, oppdatering, sletting,
/// filopplasting med versjonering og signering.
/// </summary>
public interface IDocumentService
{
    /// <summary>Henter alle dokumenter for en dokumenttype med filtrering.</summary>
    Task<Result<IReadOnlyList<DocumentListDto>>> GetAllAsync(
        string documentTypeSlug,
        Guid? currentUserId,
        Guid? documentTypeCategoryId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>Henter ett dokument basert på ID.</summary>
    Task<Result<DocumentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Oppretter et nytt dokument med valgfri filopplasting.</summary>
    Task<Result<DocumentDto>> CreateAsync(
        string documentTypeSlug,
        CreateDocumentRequest request,
        Guid uploadedById,
        string? fileName = null,
        string? contentType = null,
        Stream? fileStream = null,
        CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer metadata på et dokument.</summary>
    Task<Result<DocumentDto>> UpdateAsync(
        Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter et dokument.</summary>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Signerer et dokument for gjeldende bruker.</summary>
    Task<Result<bool>> SignAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Laster opp en ny filversjon til et dokument.</summary>
    Task<Result<DocumentDto>> UploadVersionAsync(
        Guid documentId,
        string documentTypeSlug,
        string fileName,
        string contentType,
        Stream stream,
        Guid uploadedById,
        CancellationToken cancellationToken = default);

    /// <summary>Henter fil for nedlasting.</summary>
    /// <remarks>Returnerer path slik at controlleren kan åpne streamen direkte.</remarks>
    Task<Result<DocumentDownloadResult>> GetDownloadAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Åpner en filstream for lesing. Streamen eies av calleren.</summary>
    Task<Stream> OpenFileStreamAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>Henter signaturer for et dokument.</summary>
    Task<Result<IReadOnlyList<DocumentSignatureDto>>> GetSignaturesAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Henter alle dokumenter brukeren har signert (på tvers av typer).</summary>
    Task<Result<IReadOnlyList<DocumentListDto>>> GetMySignedDocumentsAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Henter alle dokumenter brukeren trenger å signere.</summary>
    Task<Result<IReadOnlyList<DocumentListDto>>> GetMyPendingDocumentsAsync(
        Guid userId, CancellationToken cancellationToken = default);
}