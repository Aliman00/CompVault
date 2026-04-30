using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.JobTitles;
namespace CompVault.Frontend.Features.JobTitle.Models;

public class CreateJobTitleModel
{
    [Required(ErrorMessage = JobTitleValidations.Errors.NameRequired)]
    [MaxLength(JobTitleValidations.NameMaxLength, ErrorMessage = JobTitleValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    public bool IsLeader { get; set; } = false;

    public CreateJobTitleRequest ToRequest() => new()
    {
        Name = Name, 
        IsLeader = IsLeader
    };
}