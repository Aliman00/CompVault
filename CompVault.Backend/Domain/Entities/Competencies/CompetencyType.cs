using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Domain.Entities.Competencies;

/// <summary>
/// Mal for en type kompetanse, f.eks. "Førerkort klasse B" eller "HMS-kurs (årlig)".
/// Definerer om typen krever utløpsdato og tilhører en kategori.
/// </summary>
public class CompetencyType
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Egenskaper ========================

    /// <summary>Navn på kompetansetypen, f.eks. "Førerkort klasse B".</summary>
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse av kompetansetypen.</summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Kategori for gruppering i frontend, f.eks. "HMS", "Sertifikat", "Kurs".</summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Om kompetanse av denne typen krever utløpsdato.
    /// Sertifikater krever (true), introduksjonskurs krever ikke (false).
    /// </summary>
    public bool RequiresExpiration { get; set; } = true;

    // ======================== Historikk ========================

    /// <summary>Når kompetansetypen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ======================== Soft delete ========================

    /// <summary>Om kompetansetypen er aktiv.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når kompetansetypen ble soft-slettet (UTC). Null hvis aktiv.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================

    /// <summary>Alle kompetansebevis av denne typen.</summary>
    public ICollection<Competency> Competencies { get; set; } = new List<Competency>();
}