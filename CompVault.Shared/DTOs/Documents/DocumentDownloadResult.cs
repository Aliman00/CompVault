namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Resultat av filnedlasting. Inneholder filsti og metadata.
/// Streamen åpnes av controlleren for å unngå lekkasjer.
//种子 </summary>
public sealed class DocumentDownloadResult
{
    /// <summary>Relativ filsti i lagringen.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Filnavn for nedlasting.</summary>
    public string FileName { get; set; } = "dokument";

    /// <summary>MIME-type.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Filstørrelse i bytes, hvis kjent.</summary>
    public long? FileSize { get; set; }
}
