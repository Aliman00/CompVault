using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Equipment;
namespace CompVault.Frontend.Features.Equipment.Models;

public class EquipmentCategoryEditModel
{
    [Required(ErrorMessage = EquipmentValidations.Errors.NameRequired)]
    [StringLength(EquipmentValidations.CategoryNameMaxLength, ErrorMessage = EquipmentValidations.Errors.CategoryNameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [StringLength(EquipmentValidations.DescriptionMaxLength, ErrorMessage = EquipmentValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public static EquipmentCategoryEditModel FromDto(EquipmentCategoryDto dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description,
        IsActive = dto.IsActive
    };

    public UpdateEquipmentCategoryRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
        IsActive = IsActive
    };
}