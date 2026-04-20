using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Enums;
namespace CompVault.Frontend.Features.Competencies.Models;

public class CompetencyEditModel
{
    public DateTime? IssuedDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    [MaxLength(CompValidations.CertificateNumberMaxLength, ErrorMessage = CompValidations.Errors.CertNumberMaxLength)]
    public string? CertificateNumber { get; set; }

    [MaxLength(CompValidations.NotesMaxLength, ErrorMessage = CompValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }

    public CompetencyStatus Status { get; set; }

    [MaxLength(CompValidations.RevokedReasonMaxLength, ErrorMessage = CompValidations.Errors.RevokedReasonMaxLength)]
    public string? RevokedReason { get; set; }

    public static CompetencyEditModel FromDto(CompetencyDto dto) => new()
    {
        IssuedDate = dto.IssuedDate,
        ExpiryDate = dto.ExpiryDate,
        CertificateNumber = dto.CertificateNumber,
        Notes = dto.Notes,
        Status = dto.Status,
        RevokedReason = dto.RevokedReason,
    };

    public UpdateCompetencyRequest ToRequest() => new()
    {
        IssuedDate = IssuedDate,
        ExpiryDate = ExpiryDate,
        CertificateNumber = CertificateNumber,
        Notes = Notes,
    };

    public UpdateCompetencyRequest ToRevokeRequest() => new()
    {
        Status = CompetencyStatus.Revoked,
        RevokedReason = RevokedReason,
    };
}