using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Constants.Validations;
namespace CompVault.Shared.DTOs.CompetencyTypes;

/// <summary>
/// Det som sendes inn for å oppdatere en kompetansetype. Alle felt er nullable
/// for å støtte partial update.
/// </summary>
public sealed class UpdateCompetencyTypeRequest
{
    /// <summary>Nytt navn på kompetansetypen.</summary>
    [MaxLength(CompTypeValidations.NameMaxLength, ErrorMessage = CompTypeValidations.Errors.NameMaxLength)]
    public string? Name { get; set; }

    /// <summary>Ny beskrivelse.</summary>
    [MaxLength(CompTypeValidations.DescMaxLength, ErrorMessage = CompTypeValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    /// <summary>Ny kategori.</summary>
    [MaxLength(CompTypeValidations.CategoryMaxLength, ErrorMessage = CompTypeValidations.Errors.CategoryMaxLength)]
    public string? Category { get; set; }

    /// <summary>Om kompetanse av denne typen krever utløpsdato.</summary>
    public bool? RequiresExpiration { get; set; }

    /// <summary>Om kompetansetypen skal være aktiv.</summary>
    public bool? IsActive { get; set; }
}