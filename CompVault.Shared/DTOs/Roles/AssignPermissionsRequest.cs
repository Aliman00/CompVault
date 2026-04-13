using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

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
    /// Maks 50 permissions per forespørsel.
    /// </summary>
    [Required(ErrorMessage = RoleValidations.Errors.PermissionNamesRequired)]
    [MaxLength(RoleValidations.PermissionNamesMaxCount, ErrorMessage = RoleValidations.Errors.PermissionNamesMaxCount)]
    public IList<string> PermissionNames { get; set; } = new List<string>();
}