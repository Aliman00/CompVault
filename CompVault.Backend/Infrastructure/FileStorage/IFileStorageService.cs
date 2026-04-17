namespace CompVault.Backend.Infrastructure.FileStorage;

/// <summary>
/// Abstraksjon for fillagring. Muliggjør bytte mellom lokal disk, S3, Azure Blob, etc.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Lagrer en fil og returnerer stien der den lagres (relativ til root).
    /// </summary>
    /// <param name="stream">Filinnholdet.</param>
    /// <param name="relativePath">Relativ sti, f.eks. "active/doc-id/file.pdf"</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Den fullstendige relative stien til filen.</returns>
    Task<string> SaveAsync(Stream stream, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sletter en fil.
    /// </summary>
    /// <param name="relativePath">Relativ sti til filen.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sjekker om en fil eksisterer.
    /// </summary>
    /// <param name="relativePath">Relativ sti til filen.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True hvis filen eksisterer.</returns>
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flytter en fil fra kilde til mål.
    /// </summary>
    /// <param name="sourceRelativePath">Kildesti.</param>
    /// <param name="destinationRelativePath">Målsti.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MoveAsync(string sourceRelativePath, string destinationRelativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Åpner en fil for lesing.
    /// </summary>
    /// <param name="relativePath">Relativ sti til filen.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Stream for lesing.</returns>
    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Beregner SHA256-sjekksum for en fil.
    /// </summary>
    /// <param name="relativePath">Relativ sti til filen.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sjekksum som base64-streng.</returns>
    Task<string> ComputeChecksumAsync(string relativePath, CancellationToken cancellationToken = default);
}