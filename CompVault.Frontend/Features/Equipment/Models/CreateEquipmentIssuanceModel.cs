using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Equipment;

namespace CompVault.Frontend.Features.Equipment.Models;

public class CreateEquipmentIssuanceModel : IValidatableObject
{
    [Required(ErrorMessage = "Bruker er påkrevd")]
    public Guid? UserId { get; set; }
    
    [Required(ErrorMessage = "Utstyr er påkrevd")]
    public Guid? ItemId { get; set; }

    [Range(EquipmentValidations.QuantityMin, EquipmentValidations.QuantityMax,
        ErrorMessage = EquipmentValidations.Errors.QuantityRange)]
    public int Quantity { get; set; } = 1;
    
    [StringLength(EquipmentValidations.SizeMaxLength, ErrorMessage = EquipmentValidations.Errors.SizeMaxLength)]
    public string? Size { get; set; }

    public DateTime IssuedDate { get; set; } = DateTime.Today;

    [StringLength(EquipmentValidations.NotesMaxLength, ErrorMessage = EquipmentValidations.Errors.NotesMaxLength)]
    public string? Notes { get; set; }
    
    // Settes av dialogen når utstyr velges
    public bool ItemHasSize { get; set; }
    
    /// <summary>
    /// Validerer atstørrelse er påkrevd hvis utstyret krever størrelse
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ItemHasSize && string.IsNullOrWhiteSpace(Size))
            yield return new ValidationResult("Størrelse er påkrevd", [nameof(Size)]);
    }

    public CreateEquipmentIssuanceRequest ToRequest() => new()
    {
        UserId = UserId ?? Guid.Empty,
        ItemId = ItemId ?? Guid.Empty,
        Quantity = Quantity,
        Size = Size,
        IssuedDate = DateTime.SpecifyKind(IssuedDate, DateTimeKind.Utc),
        Notes = Notes
    };
}