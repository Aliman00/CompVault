using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Opprett en ny dokumenttype.
/// </summary>
public sealed class CreateDocumentTypeRequest
{
    /// <summary>Visningsnavn, f.eks. "HMS Dokumenter".</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-vennlig slug, f.eks. "hms-documents".</summary>
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>Beskrivelse.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Hvordan dokumenter retter seg mot brukere.</summary>
    [Required]
    public DocumentTargetMode TargetMode { get; set; } = DocumentTargetMode.None;

    /// <summary>Tillatte MIME-typer for opplasting.</summary>
    public string[] AllowedMimeTypes { get; set; } = ["application/pdf"];

    /// <summary>Maksimal filstørrelse i bytes. Standard: 20 MB.</summary>
    [Range(1, 100 * 1024 * 1024, ErrorMessage = "Maksimal filstørrelse må være mellom 1 byte og 100 MB.")]
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}