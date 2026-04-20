using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.CompetencyTypes;

namespace CompVault.Frontend.Features.Competencies.Models;

public class CreateCompetencyTypeModel
{
    [Required(ErrorMessage = CompTypeValidations.Errors.NameRequired)]
    [MaxLength(CompTypeValidations.NameMaxLength, ErrorMessage = CompTypeValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(CompTypeValidations.DescMaxLength, ErrorMessage = CompTypeValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    [MaxLength(CompTypeValidations.CategoryMaxLength, ErrorMessage = CompTypeValidations.Errors.CategoryMaxLength)]
    public string? Category { get; set; }

    public bool RequiresExpiration { get; set; } = true;

    public CreateCompetencyTypeRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
        Category = Category,
        RequiresExpiration = RequiresExpiration,
    };
}