using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Equipment;
namespace CompVault.Frontend.Features.Equipment.Models;

public class CreateEquipmentItemModel
{
    [Required(ErrorMessage = "Kategori er påkrevd")]
    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = EquipmentValidations.Errors.ItemNameRequired)]
    [StringLength(EquipmentValidations.ItemNameMaxLength, ErrorMessage = EquipmentValidations.Errors.ItemNameMaxLength)]
    public string Name { get; set; } = string.Empty;

    public bool HasSize { get; set; }

    public CreateEquipmentItemRequest ToRequest() => new()
    {
        CategoryId = CategoryId ?? Guid.Empty,
        Name = Name,
        HasSize = HasSize
    };
}