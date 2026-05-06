using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Domain.Entities.Equipment;

/// <summary>
/// Spesifikt utstyr under en kategori.
/// F.eks. under "Uniform": Sko, Bukse, Skjorte, Jakke.
/// F.eks. under "Verneutstyr": Hjelm, Øreklokker, Hansker.
/// </summary>
public class EquipmentItem
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Foreign keys ========================

    /// <summary>ID til kategorien dette utstyret tilhører.</summary>
    public Guid CategoryId { get; set; }

    // ======================== Egenskaper ========================

    /// <summary>Navn på utstyret, f.eks. "Sko" eller "Øreklokker".</summary>
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Om dette utstyret har størrelse.
    /// true for sko, bukser, hansker. false for hjelm, øreklokker.
    /// </summary>
    public bool HasSize { get; set; } = false;

    // ======================== Soft delete ========================

    /// <summary>Om utstyret er aktivt.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når utstyret ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Når utstyret ble soft-slettet (UTC). Null hvis aktivt.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================

    /// <summary>Kategorien dette utstyret tilhører.</summary>
    public EquipmentCategory? Category { get; set; }

    /// <summary>Alle utleveringer av dette utstyret.</summary>
    public ICollection<EquipmentIssuance> Issuances { get; set; } = new List<EquipmentIssuance>();
}