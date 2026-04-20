using System.ComponentModel.DataAnnotations;

using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Domain.Entities.Documents;

/// <summary>
/// Et dokument i systemet. Tilhører en <see cref="DocumentType"/> og kan ha filvedlegg
/// eller ekstern lenke. Støtter versjonering og signering.
/// Målgruppe settes via <see cref="DocumentDepartment"/> og <see cref="DocumentJobTitle"/>
/// koblingstabeller basert på dokumenttypens TargetMode.
/// </summary>
public class Document
{
    // ======================== Primærnøkkel ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Fremmednøkler ========================
    /// <summary>ID til dokumenttypen.</summary>
    public Guid DocumentTypeId { get; set; }

    /// <summary>ID til kategorien (DocumentTypeCategory). Null hvis ukategorisert.</summary>
    public Guid? DocumentTypeCategoryId { get; set; }

    // ======================== Innhold ========================
    /// <summary>Dokumenttittel.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Beskrivelse av dokumentet.</summary>
    public string? Description { get; set; }

    /// <summary>Ekstern URL hvis dokumentet lenker til en ekstern ressurs.</summary>
    [MaxLength(500)]
    public string? ExternalUrl { get; set; }

    // ======================== Signatur ========================
    /// <summary>Om dette dokumentet krever signering.</summary>
    public bool RequiresSignature { get; set; } = true;

    // ======================== Status ========================
    /// <summary>Om dokumentet er aktivt (ikke slettet).</summary>
    public bool IsActive { get; set; } = true;

    // ======================== Filinfo ========================
    /// <summary>Versjonsnummer. Starter på 1, økes ved hver opplasting.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Originalt filnavn.</summary>
    public string? FileName { get; set; }

    /// <summary>Sti til filen på disk.</summary>
    public string? FilePath { get; set; }

    /// <summary>Filstørrelse i bytes.</summary>
    public long? FileSize { get; set; }

    /// <summary>MIME-type.</summary>
    public string? MimeType { get; set; }

    /// <summary>SHA256-sjekksum for filintegritet.</summary>
    public string? Checksum { get; set; }

    // ======================== Metadata ========================
    /// <summary>ID til brukeren som lastet opp.</summary>
    public Guid UploadedBy { get; set; }

    /// <summary>Når dokumentet ble lastet opp (UTC).</summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Når dokumentet ble soft-slettet (UTC). Null hvis aktivt.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================
    /// <summary>Dokumenttypen.</summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>Kategorien.</summary>
    public DocumentTypeCategory? Category { get; set; }

    /// <summary>Brukeren som lastet opp.</summary>
    public ApplicationUser? Uploader { get; set; }

    /// <summary>Alle versjoner av dette dokumentet.</summary>
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    /// <summary>Alle signaturer på dette dokumentet.</summary>
    public ICollection<DocumentSignature> Signatures { get; set; } = new List<DocumentSignature>();

    /// <summary>Mål-avdelinger for dette dokumentet (mange-til-mange).</summary>
    public ICollection<DocumentDepartment> DocumentDepartments { get; set; } = new List<DocumentDepartment>();

    /// <summary>Mål-jobbtitler for dette dokumentet (mange-til-mange).</summary>
    public ICollection<DocumentJobTitle> DocumentJobTitles { get; set; } = new List<DocumentJobTitle>();
}