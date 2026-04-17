namespace CompVault.Backend.Domain.Entities.Documents;

/// <summary>
/// En brukerdefinert kategori innenfor en dokumenttype.
/// F.eks. "Nødsprosedyrer", "Sikkerhetsinstrukser" for HMS-dokumenter.
/// </summary>
public class DocumentTypeCategory
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Foreign Key ========================
    /// <summary>ID til dokumenttypen denne kategorien tilhører.</summary>
    public Guid DocumentTypeId { get; set; }

    // ======================== Egenskaper ========================
    /// <summary>Visningsnavn, f.eks. "Nødsprosedyrer".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-vennlig slug, f.eks. "emergency-procedure".</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Om kategorien er aktiv.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når kategorien ble soft-slettet (UTC). Null hvis aktiv.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================
    /// <summary>Dokumenttypen kategorien tilhører.</summary>
    public DocumentType? DocumentType { get; set; }
}