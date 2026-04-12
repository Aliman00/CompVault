using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <summary>
/// Håndterer fillagring, versjonering og sjekksum for dokumenter.
/// Ansvar for att fillagring er skilt fra DocumentService sine metadata-operasjonar.
/// </summary>
public interface IDocumentFileService
{
    /// <summary>
    /// Lagrer en fil og returnerer sti og sjekksum.
    /// </summary>
    /// <param name="stream">Filinnholdet.</param>
    /// <param name="relativePath">Relativ sti der filen skal lagres.</param>
    /// <param name="cancellationToken">Avbruddstoken.</param>
    /// <returns>Tuple med (relativ sti, sjekksum som base64).</returns>
    Task<(string FilePath, string Checksum)> SaveWithChecksumAsync(
        Stream stream, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flytter en fil fra kilde til mål. Brukes for å arkivere gamle versjoner.
    /// </summary>
    Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sletter en fil fra lagring.
    /// </summary>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sjekker om en fil eksisterer på lagring.
    /// </summary>
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Åpner en fil for lesing. Streamen eies av calleren.
    /// </summary>
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validerer MIME-type mot dokumenttypens tillatte typer.
    /// </summary>
    Result ValidateMimeType(string contentType, string[] allowedMimeTypes);

    /// <summary>
    /// Validerer filstørrelse mot dokumenttypens maksgrense.
    /// </summary>
    Result ValidateFileSize(long fileSize, long maxFileSizeBytes);
}