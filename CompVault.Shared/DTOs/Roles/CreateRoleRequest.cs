using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Roles;

/// <summary>
/// Det som sendes inn for å opprette en ny rolle.
/// </summary>
public sealed class CreateRoleRequest
{
    /// <summary>Rollens navn, f.eks. "Avdelingsleder".</summary>
    [Required]
    [MinLength(2)]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Beskrivelse av hva rollen innebærer.</summary>
    [Required]
    [MinLength(5)]
    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;
}