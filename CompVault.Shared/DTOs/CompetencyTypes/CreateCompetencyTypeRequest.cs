using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.CompetencyTypes;

/// <summary>
/// Det som sendes inn for å opprette en ny kompetansetype.
/// </summary>
public sealed class CreateCompetencyTypeRequest
{
    /// <summary>Navn på kompetansetypen.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Kategori for gruppering, f.eks. "HMS", "Sertifikat", "Kurs".</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>Om kompetanse av denne typen krever utløpsdato. Standard: true.</summary>
    public bool RequiresExpiration { get; set; } = true;
}
