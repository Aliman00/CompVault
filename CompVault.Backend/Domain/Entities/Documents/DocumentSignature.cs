using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Domain.Entities.Documents;

/// <summary>
/// En signatur på et dokument. Knyttes til en spesifikk versjon —
/// slettes ved opplasting av ny versjon slik at brukere må signere på nytt.
/// </summary>
public class DocumentSignature
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Foreign Keys ========================
    /// <summary>ID til dokumentet.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>ID til brukeren som signerte.</summary>
    public Guid UserId { get; set; }

    // ======================== Signaturinfo ========================
    /// <summary>Når signaturen ble avgitt (UTC).</summary>
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Hvilken versjon som ble signert.</summary>
    public int SignatureVersion { get; set; }

    // ======================== Navigasjonsegenskaper ========================
    /// <summary>Dokumentet som ble signert.</summary>
    public Document? Document { get; set; }

    /// <summary>Brukeren som signerte.</summary>
    public ApplicationUser? User { get; set; }
}