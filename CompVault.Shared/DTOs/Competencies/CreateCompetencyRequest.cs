using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Competencies;

/// <summary>
/// Det som sendes inn for å opprette et nytt kompetansebevis.
/// </summary>
public sealed class CreateCompetencyRequest
{
    /// <summary>ID til brukeren som skal ha kompetansebeviset.</summary>
    [Required(ErrorMessage = CompValidations.Errors.UserIdRequired)]
    public Guid? UserId { get; set; }

    /// <summary>ID til kompetansetypen.</summary>
    [Required(ErrorMessage = CompValidations.Errors.CompetencyTypeIdRequired)]
    public Guid? CompetencyTypeId { get; set; }

    /// <summary>
    /// Utløpsdato. Påkrevd hvis kompetansetypens RequiresExpiration er true.
    /// Valideres i service-laget.
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Når kompetansebeviset ble utstedt. Alltid påkrevd.</summary>
    [Required(ErrorMessage = CompValidations.Errors.IssuedDateRequired)]
    public DateTime? IssuedDate { get; set; }

    /// <summary>Valgfritt sertifikatnummer.</summary>
    [MaxLength(CompValidations.CertificateNumberMaxLength, ErrorMessage = CompValidations.Errors.CertNumberMaxLength)]
    public string? CertificateNumber { get; set; }

    /// <summary>Valgfrie notater.</summary>
    [StringLength(CompValidations.NotesMaxLength, ErrorMessage = CompValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }
}