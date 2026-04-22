namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det klienten ser når de spør etter en utstyrskategori.
/// </summary>
public sealed class EquipmentCategoryDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Navn på kategorien.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beskrivelse av kategorien.</summary>
    public string? Description { get; set; }

    /// <summary>Om kategorien er aktiv.</summary>
    public bool IsActive { get; set; }

    /// <summary>Når kategorien ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Antall utstyr under denne kategorien.</summary>
    public int ItemCount { get; set; }
}