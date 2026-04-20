using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Departments;
namespace CompVault.Frontend.Features.Departments.Models;

public class CreateDepartmentModel
{
    [Required(ErrorMessage = DepValidations.Errors.NameRequired)]
    [MaxLength(DepValidations.NameMaxLength, ErrorMessage = DepValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(DepValidations.DescriptionMaxLength, ErrorMessage = DepValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    public CreateDepartmentRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
        ParentDepartmentId = ParentDepartmentId
    };
}