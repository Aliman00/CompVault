using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Competencies;

/// <summary>
/// Det som sendes inn for å opprette et nytt kompetansebevis.
/// </summary>
public sealed class CreateCompetencyRequest
{
    /// <summary>ID til brukeren som skal ha kompetansebeviset.</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>ID til kompetansetypen.</summary>
    [Required]
    public Guid CompetencyTypeId { get; set; }

    /// <summary>
    /// Utløpsdato. Påkrevd hvis kompetansetypens RequiresExpiration er true.
    /// Valideres i service-laget.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Når kompetansebeviset ble utstedt. Alltid påkrevd.</summary>
    [Required]
    public DateTime IssuedDate { get; set; }

    /// <summary>Valgfritt sertifikatnummer.</summary>
    [MaxLength(100)]
    public string? CertificateNumber { get; set; }

    /// <summary>Valgfrie notater.</summary>
    [StringLength(2000)]
    public string? Notes { get; set; }
}
