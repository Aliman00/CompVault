namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// DTO for en dokumentsignatur.
/// </summary>
public sealed class UserSignatureStatusDto
{
    /// <summary>ID til brukeren som signerte.</summary>
    public Guid UserId { get; set; }

    /// <summary>Fullt navn på brukeren.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Målgruppen er stillingstittel. Null hvis ikke målgruppe</summary>
    public Guid? JobTitleId { get; set; }

    /// <summary>Navnet på stillingstittelen. Null hvis ikke målgruppe</summary>
    public string? JobTitleName { get; set; }

    /// <summary>Målgruppen er avdeling. Null hvis ikke målgruppe</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Navnet på avdelingen. Null hvis ikke målgruppe</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Tydelig gjør om brukeren har signert.</summary>
    public bool HasSigned { get; set; }

    /// <summary>Når signaturen ble avgitt (UTC). Null hvis ikke signert.</summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>Hvilken versjon som ble signert. Null hvis ikke signert</summary>
    public int? SignatureVersion { get; set; }

}