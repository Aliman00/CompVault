using System.ComponentModel.DataAnnotations;

using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Domain.Entities.Competencies;

/// <summary>
/// Konkret kobling mellom en ansatt og en kompetansetype.
/// Representerer ett kompetansebevis med utløpsdato, status og metadata.
/// </summary>
public sealed class Competency
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Foreign keys ========================

    /// <summary>ID til brukeren som har kompetansebeviset.</summary>
    public Guid UserId { get; set; }

    /// <summary>ID til kompetansetypen dette beviset tilhører.</summary>
    public Guid CompetencyTypeId { get; set; }

    // ======================== Egenskaper ========================

    /// <summary>Nåværende status for kompetansebeviset.</summary>
    public CompetencyStatus Status { get; set; } = CompetencyStatus.Valid;

    /// <summary>
    /// Utløpsdato for kompetansebeviset. Nullable for å støtte typer
    /// hvor <see cref="CompetencyType.RequiresExpiration"/> er false.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Når kompetansebeviset ble utstedt. Alltid påkrevd.</summary>
    public DateTime IssuedDate { get; set; }

    /// <summary>Valgfritt sertifikatnummer, f.eks. for førerkort.</summary>
    [StringLength(100)]
    public string? CertificateNumber { get; set; }

    /// <summary>Valgfrie notater knyttet til kompetansebeviset.</summary>
    public string? Notes { get; set; }

    // ======================== Revocation ========================

    /// <summary>Når beviset ble tilbakekalt (UTC). Kun satt hvis <see cref="Status"/> er Revoked.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Årsak til tilbakekalling. Påkrevt hvis status settes til Revoked.</summary>
    public string? RevokedReason { get; set; }

    // ======================== Historikk ========================

    /// <summary>Når kompetansebeviset ble opprettet i systemet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ======================== Soft delete ========================

    /// <summary>Om kompetansebeviset er aktivt.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når kompetansebeviset ble soft-slettet (UTC). Null hvis aktivt.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================

    /// <summary>Brukeren som har dette kompetansebeviset.</summary>
    public ApplicationUser? ApplicationUser { get; set; }

    /// <summary>Kompetansetypen dette beviset tilhører.</summary>
    public CompetencyType? CompetencyType { get; set; }
}
