using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det som sendes inn for å oppdatere en utstyrskategori. Alle felt er nullable
/// for å støtte partial update.
/// </summary>
public sealed class UpdateEquipmentCategoryRequest
{
    /// <summary>Nytt navn.</summary>
    [StringLength(EquipmentValidations.CategoryNameMaxLength, ErrorMessage = EquipmentValidations.Errors.CategoryNameMaxLength)]
    public string? Name { get; set; }

    /// <summary>Ny beskrivelse.</summary>
    [StringLength(EquipmentValidations.DescriptionMaxLength, ErrorMessage = EquipmentValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }

    /// <summary>Om kategorien er aktiv.</summary>
    public bool? IsActive { get; set; }
}