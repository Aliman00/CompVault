using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Oppdater en eksisterende dokumenttype.
/// </summary>
public sealed class UpdateDocumentTypeRequest
{
    /// <summary>Nytt visningsnavn.</summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>Ny beskrivelse.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Ny targeting-modus.</summary>
    public DocumentTargetMode? TargetMode { get; set; }

    /// <summary>Nye tillatte MIME-typer.</summary>
    public string[]? AllowedMimeTypes { get; set; }

    /// <summary>Ny maksimal filstørrelse i bytes.</summary>
    public long? MaxFileSizeBytes { get; set; }
}