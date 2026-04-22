using System.ComponentModel.DataAnnotations;

using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Domain.Entities.Equipment;

/// <summary>
/// Registrering av utlevert utstyr til en ansatt.
/// Representerer én utlevering med antall, størrelse og dato.
/// </summary>
public class EquipmentIssuance
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Foreign keys ========================

    /// <summary>ID til brukeren som har fått utstyret.</summary>
    public Guid UserId { get; set; }

    /// <summary>ID til utstyret som er utlevert.</summary>
    public Guid ItemId { get; set; }

    /// <summary>ID til brukeren som delte ut utstyret.</summary>
    public Guid IssuedById { get; set; }

    // ======================== Egenskaper ========================

    /// <summary>Antall utlevert, f.eks. 4 skjorter eller 2 par sko.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Størrelse, f.eks. "XL", "43", "M/L". Null hvis utstyret ikke har størrelse.</summary>
    [StringLength(20)]
    public string? Size { get; set; }

    /// <summary>Når utstyret ble utlevert.</summary>
    public DateTime IssuedDate { get; set; }

    /// <summary>Valgfrie notater knyttet til utleveringen.</summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    // ======================== Soft delete ========================

    /// <summary>Om utleveringen er aktiv.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når utleveringen ble opprettet i systemet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Når utleveringen ble soft-slettet (UTC). Null hvis aktiv.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================

    /// <summary>Brukeren som har fått utstyret.</summary>
    public ApplicationUser? User { get; set; }

    /// <summary>Utstyret som er utlevert.</summary>
    public EquipmentItem? Item { get; set; }

    /// <summary>Brukeren som delte ut utstyret.</summary>
    public ApplicationUser? IssuedBy { get; set; }
}