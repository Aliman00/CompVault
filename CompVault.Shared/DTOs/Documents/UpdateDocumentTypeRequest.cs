using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Oppdater en eksisterende dokumenttype.
/// </summary>
public sealed class UpdateDocumentTypeRequest
{
    /// <summary>Nytt visningsnavn.</summary>
    [MaxLength(DocTypeValidations.NameMaxLength, ErrorMessage = DocTypeValidations.Errors.NameMaxLength)]
    public string? Name { get; set; }

    /// <summary>Ny beskrivelse.</summary>
    [MaxLength(DocTypeValidations.DescMaxLength, ErrorMessage = DocTypeValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    /// <summary>Fjerner beskrivelsen hvis satt.</summary>
    public bool ClearDescription { get; set; }

    /// <summary>Ny targeting-modus.</summary>
    public DocumentTargetMode? TargetMode { get; set; }

    /// <summary>Nye tillatte MIME-typer.</summary>
    public string[]? AllowedMimeTypes { get; set; }

    /// <summary>Ny maksimal filstørrelse i bytes.</summary>
    [Range(1, 100 * 1024 * 1024, ErrorMessage = "Maksimal filstørrelse må være mellom 1 byte og 100 MB.")]
    public long? MaxFileSizeBytes { get; set; }
}