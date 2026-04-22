using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det som sendes inn for å opprette nytt utstyr under en kategori.
/// </summary>
public sealed class CreateEquipmentItemRequest
{
    /// <summary>ID til kategorien utstyret tilhører.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Navn på utstyret, f.eks. "Sko" eller "Hjelm".</summary>
    [Required(ErrorMessage = EquipmentValidations.Errors.ItemNameRequired)]
    [StringLength(EquipmentValidations.ItemNameMaxLength, ErrorMessage = EquipmentValidations.Errors.ItemNameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Om dette utstyret har størrelse. Standard er false.</summary>
    public bool HasSize { get; set; }
}