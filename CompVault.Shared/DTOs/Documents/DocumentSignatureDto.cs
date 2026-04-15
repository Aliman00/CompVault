namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// DTO for en dokumentsignatur.
/// </summary>
public sealed class DocumentSignatureDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>ID til dokumentet.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>ID til brukeren som signerte.</summary>
    public Guid UserId { get; set; }

    /// <summary>Fullt navn på brukeren.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Når signaturen ble avgitt (UTC).</summary>
    public DateTime SignedAt { get; set; }

    /// <summary>Hvilken versjon som ble signert.</summary>
    public int SignatureVersion { get; set; }
}