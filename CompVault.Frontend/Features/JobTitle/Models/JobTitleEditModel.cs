using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.JobTitles;
namespace CompVault.Frontend.Features.JobTitle.Models;

public class JobTitleEditModel
{
    [Required(ErrorMessage = JobTitleValidations.Errors.NameRequired)]
    [MaxLength(JobTitleValidations.NameMaxLength, ErrorMessage = JobTitleValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
    
    public bool IsActive { get; set; }

    public static JobTitleEditModel FromDto(JobTitleDto dto) => new()
    {
        Name = dto.Name,
        IsActive = dto.IsActive
    };

    public UpdateJobTitleRequest ToRequest() => new()
    {
        Name = Name, 
        IsActive = IsActive
    };
}