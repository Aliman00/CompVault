using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Opprett et nytt dokument.
/// </summary>
public sealed class CreateDocumentRequest
{
    /// <summary>Dokumenttittel.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Beskrivelse av dokumentet.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>ID til kategorien (DocumentTypeCategory). Null = ukategorisert.</summary>
    public Guid? DocumentTypeCategoryId { get; set; }

    /// <summary>Ekstern URL i stedet for filopplasting.</summary>
    [MaxLength(500)]
    [Url]
    public string? ExternalUrl { get; set; }

    /// <summary>ID til målavdelingen. Brukes kun når DocumentType.TargetMode er Department.</summary>
    public Guid? TargetDepartmentId { get; set; }

    /// <summary>Mål-jobbtittel. Brukes kun når DocumentType.TargetMode er JobTitle.</summary>
    [MaxLength(100)]
    public string? TargetJobTitle { get; set; }

    /// <summary>Om dokumentet krever signering. Standard er true.</summary>
    public bool RequiresSignature { get; set; } = true;
}