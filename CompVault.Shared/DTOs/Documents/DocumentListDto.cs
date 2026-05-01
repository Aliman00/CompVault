namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// DTO for listevisning med signaturstatus.
/// </summary>
public sealed class DocumentListDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Dokumenttittel.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Slug til dokumenttypen</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Navn for å vise i UI-en til dokumenttypen</summary>
    public string SlugName { get; set; } = string.Empty;

    /// <summary>Beskrivelse.</summary>
    public string? Description { get; set; }

    /// <summary>ID til kategorien (DocumentTypeCategory).</summary>
    public Guid? DocumentTypeCategoryId { get; set; }

    /// <summary>Kategorinavnet.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Ekstern URL.</summary>
    public string? ExternalUrl { get; set; }

    /// <summary>Om dokumentet har en tilknyttet fil.</summary>
    public bool HasFile { get; set; }

    /// <summary>Filnavn.</summary>
    public string? FileName { get; set; }

    /// <summary>ID-er til mål-avdelinger.</summary>
    public List<Guid> TargetDepartmentIds { get; set; } = [];

    /// <summary>Navn på mål-avdelinger.</summary>
    public List<string> TargetDepartmentNames { get; set; } = [];

    /// <summary>ID-er til mål-jobbtitler.</summary>
    public List<Guid> TargetJobTitleIds { get; set; } = [];

    /// <summary>Navn på mål-jobbtitler.</summary>
    public List<string> TargetJobTitleNames { get; set; } = [];

    /// <summary>Om dokumentet krever signering.</summary>
    public bool RequiresSignature { get; set; }

    /// <summary>Versjonsnummer.</summary>
    public int Version { get; set; }

    /// <summary>Navn på brukeren som lastet opp.</summary>
    public string? UploadedByName { get; set; }

    /// <summary>Når dokumentet ble lastet opp (UTC).</summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>Antall brukere som har signert gjeldende versjon.</summary>
    public int TotalSignatures { get; set; }

    /// <summary>Om gjeldende bruker har signert.</summary>
    public bool SignedByCurrentUser { get; set; }
}