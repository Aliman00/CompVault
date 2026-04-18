using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Competencies;
namespace CompVault.Frontend.Features.Competencies.Models;

public class CreateCompetencyModel
{
    [Required(ErrorMessage = CompValidations.Errors.UserIdRequired)]
    public Guid? UserId { get; set; }

    [Required(ErrorMessage = CompValidations.Errors.CompetencyTypeIdRequired)]
    public Guid? CompetencyTypeId { get; set; }

    [Required(ErrorMessage = CompValidations.Errors.IssuedDateRequired)]
    public DateTime? IssuedDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(CompValidations.CertificateNumberMaxLength, ErrorMessage = CompValidations.Errors.CertNumberMaxLength)]
    public string? CertificateNumber { get; set; }

    [MaxLength(CompValidations.NotesMaxLength, ErrorMessage = CompValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }

    public CreateCompetencyRequest ToRequest() => new()
    {
        UserId = UserId,
        CompetencyTypeId = CompetencyTypeId,
        IssuedDate = IssuedDate.HasValue ? DateTime.SpecifyKind(IssuedDate.Value, DateTimeKind.Utc) : null,
        ExpiryDate = ExpiryDate.HasValue ? DateTime.SpecifyKind(ExpiryDate.Value, DateTimeKind.Utc) : null,
        CertificateNumber = CertificateNumber,
        Notes = Notes,
    };
}