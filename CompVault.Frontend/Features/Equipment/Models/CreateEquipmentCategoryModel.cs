using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Equipment;
namespace CompVault.Frontend.Features.Equipment.Models;

public class CreateEquipmentCategoryModel
{
    [Required(ErrorMessage = EquipmentValidations.Errors.NameRequired)]
    [StringLength(EquipmentValidations.CategoryNameMaxLength, ErrorMessage = EquipmentValidations.Errors.CategoryNameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(EquipmentValidations.DescriptionMaxLength, ErrorMessage = EquipmentValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }

    public CreateEquipmentCategoryRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description
    };
}