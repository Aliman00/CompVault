using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Equipment;

namespace CompVault.Frontend.Features.Equipment.Models;

public class EquipmentItemEditModel
{
    [Required(ErrorMessage = EquipmentValidations.Errors.ItemNameRequired)]
    [StringLength(EquipmentValidations.ItemNameMaxLength, ErrorMessage = EquipmentValidations.Errors.ItemNameMaxLength)]
    public string Name { get; set; } = string.Empty;

    public bool HasSize { get; set; }

    public bool IsActive { get; set; }

    public static EquipmentItemEditModel FromDto(EquipmentItemDto dto) => new()
    {
        Name = dto.Name,
        HasSize = dto.HasSize,
        IsActive = dto.IsActive
    };

    public UpdateEquipmentItemRequest ToRequest() => new()
    {
        Name = Name,
        HasSize = HasSize,
        IsActive = IsActive
    };
}