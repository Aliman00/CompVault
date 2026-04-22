namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det klienten ser når de spør etter et utstyr.
/// </summary>
public sealed class EquipmentItemDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>ID til kategorien.</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Navn på kategorien utstyret tilhører.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Navn på utstyret.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Om utstyret har størrelse.</summary>
    public bool HasSize { get; set; }

    /// <summary>Om utstyret er aktivt.</summary>
    public bool IsActive { get; set; }

    /// <summary>Når utstyret ble opprettet i systemet (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}