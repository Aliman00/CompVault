using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Roles;

/// <summary>
/// Det som sendes inn for å oppdatere en eksisterende rolle.
/// Alle felt er nullable for partial update.
/// </summary>
public sealed class UpdateRoleRequest
{
    /// <summary>Rollens navn.</summary>
    [MinLength(2)]
    [MaxLength(256)]
    public string? Name { get; set; }

    /// <summary>Beskrivelse av hva rollen innebærer.</summary>
    [MinLength(5)]
    [MaxLength(250)]
    public string? Description { get; set; }
}