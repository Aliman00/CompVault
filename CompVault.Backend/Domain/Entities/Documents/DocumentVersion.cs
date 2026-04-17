namespace CompVault.Backend.Domain.Entities.Documents;

/// <summary>
/// En historisk versjon av et dokument.
/// Arkiveres automatisk ved opplasting av ny versjon.
/// </summary>
public class DocumentVersion
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Foreign Key ========================
    /// <summary>ID til hoveddokumentet.</summary>
    public Guid DocumentId { get; set; }

    // ======================== Versjoninfo ========================
    /// <summary>Versjonsnummer.</summary>
    public int Version { get; set; }

    // ======================== Filinfo ========================
    /// <summary>Originalt filnavn.</summary>
    public string? FileName { get; set; }

    /// <summary>Sti til filen.</summary>
    public string? FilePath { get; set; }

    /// <summary>Filstørrelse i bytes.</summary>
    public long? FileSize { get; set; }

    /// <summary>MIME-type.</summary>
    public string? MimeType { get; set; }

    /// <summary>SHA256-sjekksum.</summary>
    public string? Checksum { get; set; }

    // ======================== Metadata ========================
    /// <summary>Når versjonen ble arkivert (UTC).</summary>
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;

    // ======================== Navigasjonsegenskaper ========================
    /// <summary>Hoveddokumentet.</summary>
    public Document? Document { get; set; }
}