using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det som sendes inn for å opprette en ny utlevering.
/// </summary>
public sealed class CreateEquipmentIssuanceRequest
{
    /// <summary>ID til brukeren som skal ha utstyret.</summary>
    public Guid UserId { get; set; }

    /// <summary>ID til utstyret som skal utleveres.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Antall som utleveres. Standard er 1.</summary>
    [Range(EquipmentValidations.QuantityMin, EquipmentValidations.QuantityMax, ErrorMessage =
        EquipmentValidations.Errors.QuantityRange)]
    public int Quantity { get; set; } = 1;

    /// <summary>Størrelse, f.eks. "XL" eller "43".</summary>
    [StringLength(EquipmentValidations.SizeMaxLength, ErrorMessage = EquipmentValidations.Errors.SizeMaxLength)]
    public string? Size { get; set; }

    /// <summary>Når utstyret ble utlevert.</summary>
    public DateTime IssuedDate { get; set; }

    /// <summary>Valgfrie notater.</summary>
    [StringLength(EquipmentValidations.NotesMaxLength, ErrorMessage = EquipmentValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }
}