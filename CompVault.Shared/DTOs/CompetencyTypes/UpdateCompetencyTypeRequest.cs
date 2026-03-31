using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.CompetencyTypes;

/// <summary>
/// Det som sendes inn for å oppdatere en kompetansetype. Alle felt er nullable
/// for å støtte partial update.
/// </summary>
public sealed class UpdateCompetencyTypeRequest
{
    /// <summary>Nytt navn på kompetansetypen.</summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>Ny beskrivelse.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Ny kategori.</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>Om kompetanse av denne typen krever utløpsdato.</summary>
    public bool? RequiresExpiration { get; set; }

    /// <summary>Om kompetansetypen skal være aktiv.</summary>
    public bool? IsActive { get; set; }
}
