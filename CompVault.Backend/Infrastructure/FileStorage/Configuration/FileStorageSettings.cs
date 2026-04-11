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
    /// </summary>
    public IReadOnlyList<string> AllowedMimeTypes { get; set; } = new List<string>
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    /// <summary>
    /// Maksimalt antall versjoner per dokument.
    /// </summary>
    public int MaxVersionsPerDocument { get; set; } = 20;
}