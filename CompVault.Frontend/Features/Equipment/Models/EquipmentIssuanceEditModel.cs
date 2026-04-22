using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Equipment;
namespace CompVault.Frontend.Features.Equipment.Models;

public class EquipmentIssuanceEditModel
{
    [Range(EquipmentValidations.QuantityMin, EquipmentValidations.QuantityMax, 
        ErrorMessage = EquipmentValidations.Errors.QuantityRange)]
    public int Quantity { get; set; } = 1;

    [StringLength(EquipmentValidations.SizeMaxLength, ErrorMessage = EquipmentValidations.Errors.SizeMaxLength)]
    public string? Size { get; set; }

    [StringLength(EquipmentValidations.NotesMaxLength, ErrorMessage = EquipmentValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }

    public static EquipmentIssuanceEditModel FromDto(EquipmentIssuanceDto dto) => new()
    {
        Quantity = dto.Quantity,
        Size = dto.Size,
        Notes = dto.Notes
    };

    public UpdateEquipmentIssuanceRequest ToRequest() => new()
    {
        Quantity = Quantity,
        Size = Size,
        Notes = Notes
    };
}