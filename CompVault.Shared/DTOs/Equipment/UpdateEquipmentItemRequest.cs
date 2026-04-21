using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det som sendes inn for å oppdatere et utstyr. Alle felt er nullable
/// for å støtte partial update.
/// </summary>
public sealed class UpdateEquipmentItemRequest
{
    /// <summary>Nytt navn.</summary>
    [StringLength(EquipmentValidations.ItemNameMaxLength, ErrorMessage = EquipmentValidations.Errors.ItemNameMaxLength)]
    public string? Name { get; set; }

    /// <summary>Om utstyret har størrelse.</summary>
    public bool? HasSize { get; set; }

    /// <summary>Om utstyret er aktivt.</summary>
    public bool? IsActive { get; set; }
}