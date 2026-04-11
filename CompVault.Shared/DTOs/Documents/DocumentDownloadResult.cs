namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Resultat av filnedlasting. Inneholder stream og metadata.
/// </summary>
public sealed class DocumentDownloadResult
{
    /// <summary>Filinnhold som stream.</summary>
    public Stream Stream { get; set; } = Stream.Null;

    /// <summary>Filnavn for nedlasting.</summary>
    public string FileName { get; set; } = "dokument";

    /// <summary>MIME-type.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Filstørrelse i bytes, hvis kjent.</summary>
    public long? FileSize { get; set; }
}