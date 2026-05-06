using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Domain.Entities.Equipment;

/// <summary>
/// Kategori for utstyr, f.eks. "Uniform" eller "Verneutstyr".
/// Opprettes av bedriften for å organisere utstyr.
/// </summary>
public class EquipmentCategory
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Egenskaper ========================

    /// <summary>Navn på kategorien, f.eks. "Uniform" eller "Verneutstyr".</summary>
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse av kategorien.</summary>
    [StringLength(300)]
    public string? Description { get; set; }

    // ======================== Soft delete ========================

    /// <summary>Om kategorien er aktiv.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når kategorien ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Når kategorien ble soft-slettet (UTC). Null hvis aktiv.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================

    /// <summary>Alle utstyr under denne kategorien.</summary>
    public ICollection<EquipmentItem> Items { get; set; } = new List<EquipmentItem>();
}