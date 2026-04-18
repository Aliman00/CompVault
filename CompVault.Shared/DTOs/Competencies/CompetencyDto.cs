using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Competencies;

/// <summary>
/// Det klienten ser når de spør etter et kompetansebevis.
/// Inkluderer navigasjonsdata (bruker- og typenavn) og beregnet dager til utløp.
/// </summary>
public sealed class CompetencyDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>ID til brukeren som har kompetansebeviset.</summary>
    public Guid UserId { get; set; }

    /// <summary>Brukerens fornavn.</summary>
    public string? UserFirstName { get; set; }

    /// <summary>Brukerens etternavn.</summary>
    public string? UserLastName { get; set; }
    
    /// <summary>Fullt navn — satt sammen automatisk.</summary>
    public string FullName => $"{UserFirstName} {UserLastName}".Trim();

    /// <summary>ID til kompetansetypen.</summary>
    public Guid CompetencyTypeId { get; set; }

    /// <summary>Navn på kompetansetypen.</summary>
    public string? CompetencyTypeName { get; set; }

    /// <summary>Om typen krever utløpsdato.</summary>
    public bool CompetencyTypeRequiresExpiration { get; set; }

    /// <summary>Nåværende status.</summary>
    public CompetencyStatus Status { get; set; }

    /// <summary>Utløpsdato (null hvis typen ikke krever utløp).</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Når beviset ble utstedt.</summary>
    public DateTime IssuedDate { get; set; }

    /// <summary>Valgfritt sertifikatnummer.</summary>
    public string? CertificateNumber { get; set; }

    /// <summary>Valgfrie notater.</summary>
    public string? Notes { get; set; }

    /// <summary>Antall dager til utløp (null hvis ingen utløpsdato).</summary>
    public int? DaysUntilExpiry { get; set; }

    /// <summary>Når kompetansebeviset ble opprettet i systemet (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Når beviset ble tilbakekalt (UTC). Null hvis ikke tilbakekalt.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Årsak til tilbakekalling.</summary>
    public string? RevokedReason { get; set; }
}