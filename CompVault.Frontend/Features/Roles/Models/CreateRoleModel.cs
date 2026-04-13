using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Roles;

namespace CompVault.Frontend.Features.Roles.Models;

public class CreateRoleModel
{
    [Required(ErrorMessage = RoleValidations.Errors.NameRequired)]
    [MinLength(RoleValidations.NameMinLength, ErrorMessage = RoleValidations.Errors.NameMinLength)]
    [MaxLength(RoleValidations.NameMaxLength, ErrorMessage = RoleValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = RoleValidations.Errors.DescriptionRequired)]
    [MinLength(RoleValidations.DescriptionMinLength, ErrorMessage = RoleValidations.Errors.DescriptionMinLength)]
    [MaxLength(RoleValidations.DescriptionMaxLength, ErrorMessage = RoleValidations.Errors.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;

    public CreateRoleRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
    };
}