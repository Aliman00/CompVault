using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
namespace CompVault.Shared.DTOs.CompetencyTypes;

/// <summary>
/// Det som sendes inn for å opprette en ny kompetansetype.
/// </summary>
public sealed class CreateCompetencyTypeRequest
{
    /// <summary>Navn på kompetansetypen.</summary>
    [Required(ErrorMessage = CompTypeValidations.Errors.NameRequired)]
    [MaxLength(CompTypeValidations.NameMaxLength,
        ErrorMessage = CompTypeValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse.</summary>
    [MaxLength(CompTypeValidations.DescMaxLength, ErrorMessage = CompTypeValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    /// <summary>Kategori for gruppering, f.eks. "HMS", "Sertifikat", "Kurs".</summary>
    [MaxLength(CompTypeValidations.CategoryMaxLength, ErrorMessage = CompTypeValidations.Errors.CategoryMaxLength)]
    public string? Category { get; set; }

    /// <summary>Om kompetanse av denne typen krever utløpsdato. Standard: true.</summary>
    public bool RequiresExpiration { get; set; } = true;
}