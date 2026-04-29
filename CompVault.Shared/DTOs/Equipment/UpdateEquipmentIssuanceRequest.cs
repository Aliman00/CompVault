using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det som sendes inn for å oppdatere en utlevering. Alle felt er nullable
/// for å støtte partial update.
/// </summary>
public sealed class UpdateEquipmentIssuanceRequest
{
    /// <summary>Nytt antall.</summary>
    [Range(EquipmentValidations.QuantityMin, EquipmentValidations.QuantityMax, 
        ErrorMessage = EquipmentValidations.Errors.QuantityRange)]
    public int? Quantity { get; set; }

    /// <summary>Ny størrelse.</summary>
    [StringLength(EquipmentValidations.SizeMaxLength, ErrorMessage = EquipmentValidations.Errors.SizeMaxLength)]
    public string? Size { get; set; }

    /// <summary>Nye notater.</summary>
    [StringLength(EquipmentValidations.NotesMaxLength, ErrorMessage = EquipmentValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }
}