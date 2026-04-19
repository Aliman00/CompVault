using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <summary>
/// Håndterer filversjonering for dokumenter: opplasting av nye versjoner,
/// nedlasting og filsstrømming.
/// </summary>
public interface IDocumentVersioningService
{
    /// <summary>
    /// Laster opp en ny filversjon til et dokument. Håndterer fil-lagring,
    /// sjekksum-validering, arkivering av gammel versjon og DB-oppdatering
    /// som én atomisk operasjon.
    /// </summary>
    Task<Result<DocumentDto>> UploadVersionAsync(
        Guid documentId,
        string documentTypeSlug,
        string fileName,
        string contentType,
        Stream stream,
        Guid uploadedById,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter fil-metadata for nedlasting. Returnerer sti, filnavn, content-type og størrelse.
    /// Sjekker også tilgang via målgruppe hvis bypassTargeting er false.
    /// </summary>
    Task<Result<DocumentDownloadResult>> GetDownloadAsync(
        Guid documentId, Guid? currentUserId = null, bool bypassTargeting = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Åpner en filstream for lesing. Streamen eies av calleren.
    /// </summary>
    Task<Stream> OpenFileStreamAsync(string relativePath, CancellationToken cancellationToken = default);
}