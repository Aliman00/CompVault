using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <summary>
/// Håndterer signatur-operasjoner for dokumenter: signering, henting av signaturer,
/// og visning av dokumenter brukeren har signert eller venter på å signere.
/// </summary>
public interface IDocumentSignatureService
{
    /// <summary>Signerer et dokument for gjeldende bruker.</summary>
    Task<Result<bool>> SignAsync(Guid documentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Henter signaturer for et dokument (gjeldende versjon).</summary>
    Task<Result<IReadOnlyList<DocumentSignatureDto>>> GetSignaturesAsync(
        Guid documentId, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default);

    /// <summary>Henter alle dokumenter brukeren har signert (på tvers av typer).</summary>
    Task<Result<IReadOnlyList<DocumentListDto>>> GetMySignedDocumentsAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Henter alle dokumenter brukeren trenger å signere.</summary>
    Task<Result<IReadOnlyList<DocumentListDto>>> GetMyPendingDocumentsAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Henter fremdriftsstatistikk for en dokumenttype for en spesifikk bruker.</summary>
    Task<Result<DocumentProgressDto>> GetProgressAsync(
        string documentTypeSlug, Guid userId, CancellationToken cancellationToken = default);
}