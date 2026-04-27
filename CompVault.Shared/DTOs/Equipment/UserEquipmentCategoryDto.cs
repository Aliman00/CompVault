namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// DTO for å vise kun de nødvendige feltene på en Overview-side
/// </summary>
public sealed class UserEquipmentCategoryDto
{
    /// <summary> ID-en til ustyrskategorien </summary>
    public Guid Id { get; set; }
    
    /// <summary> Navnet til ustyrskategorien </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary> Antall utstyrt utlevert til brukeren i denne kategorien </summary>
    public int ItemCount { get; set; }
}