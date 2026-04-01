namespace CompVault.Shared.DTOs.CompetencyTypes;

/// <summary>
/// Det klienten ser når de spør etter en kompetansetype.
/// </summary>
public sealed class CompetencyTypeDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Navn på kompetansetypen.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beskrivelse av kompetansetypen.</summary>
    public string? Description { get; set; }

    /// <summary>Kategori for gruppering, f.eks. "HMS", "Sertifikat", "Kurs".</summary>
    public string? Category { get; set; }

    /// <summary>Om kompetanse av denne typen krever utløpsdato.</summary>
    public bool RequiresExpiration { get; set; }

    /// <summary>Når kompetansetypen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Om kompetansetypen er aktiv.</summary>
    public bool IsActive { get; set; }
}