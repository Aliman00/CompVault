namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Fullstendig DTO for ett dokument.
/// </summary>
public sealed class DocumentDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>ID til dokumenttypen.</summary>
    public Guid DocumentTypeId { get; set; }

    /// <summary>Dokumenttypens slug, f.eks. "hms-documents".</summary>
    public string DocumentTypeSlug { get; set; } = string.Empty;

    /// <summary>ID til kategorien (DocumentTypeCategory).</summary>
    public Guid? DocumentTypeCategoryId { get; set; }

    /// <summary>Kategorinavnet.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Dokumenttittel.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Beskrivelse.</summary>
    public string? Description { get; set; }

    /// <summary>Ekstern URL.</summary>
    public string? ExternalUrl { get; set; }

    /// <summary>ID til målavdelingen.</summary>
    public Guid? TargetDepartmentId { get; set; }

    /// <summary>ID til mål-jobbtittelen.</summary>
    public Guid? TargetJobTitleId { get; set; }

    /// <summary>Navn på mål-jobbtittelen.</summary>
    public string? TargetJobTitleName { get; set; }

    /// <summary>Om dokumentet krever signering.</summary>
    public bool RequiresSignature { get; set; }

    /// <summary>Om dokumentet har en tilknyttet fil.</summary>
    public bool HasFile { get; set; }

    /// <summary>Versjonsnummer.</summary>
    public int Version { get; set; }

    /// <summary>Filnavn.</summary>
    public string? FileName { get; set; }

    /// <summary>Filstørrelse i bytes.</summary>
    public long? FileSize { get; set; }

    /// <summary>MIME-type.</summary>
    public string? MimeType { get; set; }

    /// <summary>Om dokumentet er aktivt.</summary>
    public bool IsActive { get; set; }

    /// <summary>ID til brukeren som lastet opp.</summary>
    public Guid UploadedBy { get; set; }

    /// <summary>Navn på brukeren som lastet opp.</summary>
    public string? UploadedByName { get; set; }

    /// <summary>Når dokumentet ble lastet opp (UTC).</summary>
    public DateTime UploadedAt { get; set; }
}