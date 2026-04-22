using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det som sendes inn for å opprette en ny utstyrskategori.
/// </summary>
public sealed class CreateEquipmentCategoryRequest
{
    /// <summary>Navn på kategorien, f.eks. "Uniform".</summary>
    [Required(ErrorMessage = EquipmentValidations.Errors.NameRequired)]
    [StringLength(EquipmentValidations.CategoryNameMaxLength, ErrorMessage = EquipmentValidations.Errors.CategoryNameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse.</summary>
    [StringLength(EquipmentValidations.DescriptionMaxLength, ErrorMessage = EquipmentValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }
}