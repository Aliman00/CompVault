using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Departments;

/// <summary>
/// Det som sendes inn for å opprette en ny avdeling.
/// </summary>
public sealed class CreateDepartmentRequest
{
    /// <summary>Avdelingens navn.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse av hva avdelingen driver med.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>ID til overordnet avdeling (valgfritt — null = toppnivå).</summary>
    public Guid? ParentDepartmentId { get; set; }
}
