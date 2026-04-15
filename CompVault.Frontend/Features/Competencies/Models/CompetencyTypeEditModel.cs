using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.CompetencyTypes;

namespace CompVault.Frontend.Features.Competencies.Models;

public class CompetencyTypeEditModel
{
    [MaxLength(CompTypeValidations.NameMaxLength, ErrorMessage = CompTypeValidations.Errors.NameMaxLength)]
    public string? Name { get; set; }

    [MaxLength(CompTypeValidations.DescMaxLength, ErrorMessage = CompTypeValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    [MaxLength(CompTypeValidations.CategoryMaxLength, ErrorMessage = CompTypeValidations.Errors.CategoryMaxLength)]
    public string? Category { get; set; }

    public bool? RequiresExpiration { get; set; }

    public bool? IsActive { get; set; }

    public static CompetencyTypeEditModel FromDto(CompetencyTypeDto dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description,
        Category = dto.Category,
        RequiresExpiration = dto.RequiresExpiration,
        IsActive = dto.IsActive,
    };

    public UpdateCompetencyTypeRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
        Category = Category,
        RequiresExpiration = RequiresExpiration,
        IsActive = IsActive,
    };
}