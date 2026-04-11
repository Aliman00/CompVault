namespace CompVault.Backend.Infrastructure.FileStorage.Configuration;

/// <summary>
/// Innstillinger for fillagring.
/// </summary>
public sealed class FileStorageSettings
{
    /// <summary>
    /// Rotmappen der alle filer lagres.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Maksimal tillatt filstørrelse i bytes.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024; // 20 MB

    /// <summary>
    /// Liste over tillatte MIME-typer for opplasting.
    /// Inkluderer PDF, Word-dokumenter, Excel-arkiver, og vanlige bildeformat.
    /// </summary>
    public IReadOnlyList<string> AllowedMimeTypes { get; set; } = new List<string>
    {
        // PDF
        "application/pdf",
        // Microsoft Word
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        // Microsoft Excel
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        // Tekst
        "text/plain",
        "text/csv",
        // Bilder (for dokumenter med innebygde bilder)
        "image/png",
        "image/jpeg",
    };

    /// <summary>
    /// Maksimalt antall versjoner per dokument.
    /// </summary>
    public int MaxVersionsPerDocument { get; set; } = 20;
}
