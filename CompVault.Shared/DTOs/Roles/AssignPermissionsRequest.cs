using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Roles;

/// <summary>
/// Det som sendes inn for å tilordne permissions til en rolle.
/// Overskriver eksisterende permissions.
/// </summary>
public sealed class AssignPermissionsRequest
{
    /// <summary>
    /// Lista over permission-navn som skal tildeles rollen.
    /// F.eks. ["users:read", "users:write", "departments:read"].
    /// </summary>
    [Required]
    [MaxLength(50)]
    public IReadOnlyList<string> PermissionNames { get; set; } = new List<string>();
}