using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Departments;

/// <summary>
/// Det som sendes inn for å oppdatere en avdeling. Alle felt er nullable
/// for å støtte partial update.
/// </summary>
public sealed class UpdateDepartmentRequest
{
    /// <summary>Nytt navn på avdelingen.</summary>
    [MaxLength(200)]
    public string? Name { get; set; }

    /// <summary>Ny beskrivelse.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Ny overordnet avdeling.</summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>Sett til true for å fjerne overordnet avdeling (flytte til toppnivå).</summary>
    public bool ClearParentDepartment { get; set; }

    /// <summary>Om avdelingen skal være aktiv.</summary>
    public bool? IsActive { get; set; }
}