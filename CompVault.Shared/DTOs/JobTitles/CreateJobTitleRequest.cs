using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.JobTitles;

/// <summary>
/// Opprett en ny stillingstittel.
/// </summary>
public sealed class CreateJobTitleRequest
{
    /// <summary>Navn på stillingstittelen.</summary>
    [Required(ErrorMessage = JobTitleValidations.Errors.NameRequired)]
    [MaxLength(JobTitleValidations.NameMaxLength, ErrorMessage = JobTitleValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Om stillinger med denne tittelen skal regnes som ledere.</summary>
    public bool IsLeader { get; set; } = false;
}