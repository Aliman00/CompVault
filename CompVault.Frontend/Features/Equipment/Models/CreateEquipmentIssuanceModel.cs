using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Equipment;

namespace CompVault.Frontend.Features.Equipment.Models;

public class CreateEquipmentIssuanceModel
{
    public Guid UserId { get; set; }

    public Guid ItemId { get; set; }

    [Range(EquipmentValidations.QuantityMin, EquipmentValidations.QuantityMax,
        ErrorMessage = EquipmentValidations.Errors.QuantityRange)]
    public int Quantity { get; set; } = 1;

    [StringLength(EquipmentValidations.SizeMaxLength, ErrorMessage = EquipmentValidations.Errors.SizeMaxLength)]
    public string? Size { get; set; }

    public DateTime IssuedDate { get; set; } = DateTime.Today;

    [StringLength(EquipmentValidations.NotesMaxLength, ErrorMessage = EquipmentValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }

    public CreateEquipmentIssuanceRequest ToRequest() => new()
    {
        UserId = UserId,
        ItemId = ItemId,
        Quantity = Quantity,
        Size = Size,
        IssuedDate = IssuedDate,
        Notes = Notes
    };
}