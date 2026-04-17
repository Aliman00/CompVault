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
}