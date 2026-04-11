using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Domain.Entities.Documents;

/// <summary>
/// En dokumenttype definert av bedriften, f.eks. "HMS Dokumenter", "Stillingsinstrukser", "Kursmateriell".
/// Hver type har egne kategorier, targeting-regler og tilgangskontroll.
/// </summary>
public class DocumentType
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Identifikasjon ========================
    /// <summary>Visningsnavn, f.eks. "HMS Dokumenter".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-vennlig slug, f.eks. "hms-documents". Unik i systemet.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Beskrivelse av dokumenttypen.</summary>
    public string? Description { get; set; }

    // ======================== Targeting ========================
    /// <summary>Hvordan dokumenter av denne typen retter seg mot brukere.</summary>
    public DocumentTargetMode TargetMode { get; set; } = DocumentTargetMode.None;

    // ======================== Filkonfigurasjon ========================
    /// <summary>Undermappe i fillagring for denne dokumenttypen.</summary>
    public string StorageFolder { get; set; } = string.Empty;

    /// <summary>Tillatte MIME-typer for opplasting.</summary>
    public string[] AllowedMimeTypes { get; set; } = [];

    /// <summary>Maksimal filstørrelse i bytes.</summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    // ======================== Status ========================
    /// <summary>Om dokumenttypen er aktiv og synlig.</summary>
    public bool IsActive { get; set; } = true;

    // ======================== Metadata ========================
    /// <summary>Når dokumenttypen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hvem som opprettet dokumenttypen.</summary>
    public Guid? CreatedById { get; set; }

    /// <summary>Brukeren som opprettet dokumenttypen.</summary>
    public ApplicationUser? CreatedBy { get; set; }

    // ======================== Soft delete ========================
    /// <summary>Når dokumenttypen ble soft-slettet (UTC). Null hvis aktiv.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================
    /// <summary>Alle kategorier for denne dokumenttypen.</summary>
    public ICollection<DocumentTypeCategory> Categories { get; set; } = new List<DocumentTypeCategory>();

    /// <summary>Alle dokumenter av denne typen.</summary>
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}