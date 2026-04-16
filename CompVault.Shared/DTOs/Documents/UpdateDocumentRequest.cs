using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Oppdater metadata på et eksisterende dokument.
/// </summary>
public sealed class UpdateDocumentRequest
{
    /// <summary>Ny tittel.</summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>Ny beskrivelse.</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Ny kategori-ID (DocumentTypeCategory).</summary>
    public Guid? DocumentTypeCategoryId { get; set; }

    /// <summary>Ny ekstern URL.</summary>
    [MaxLength(500)]
    [Url]
    public string? ExternalUrl { get; set; }

    /// <summary>Ny liste med mål-avdelinger. Null = ikke endret. Tom liste = fjern alle.</summary>
    public List<Guid>? TargetDepartmentIds { get; set; }

    /// <summary>Ny liste med mål-jobbtitler. Null = ikke endret. Tom liste = fjern alle.</summary>
    public List<Guid>? TargetJobTitleIds { get; set; }

    /// <summary>Sett til true for å fjerne ExternalUrl.</summary>
    public bool ClearExternalUrl { get; set; }

    /// <summary>Sett til true for å fjerne DocumentTypeCategoryId.</summary>
    public bool ClearDocumentTypeCategoryId { get; set; }

    /// <summary>Om dokumentet krever signering. Null = ikke endret.</summary>
    public bool? RequiresSignature { get; set; }
}