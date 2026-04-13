using CompVault.Shared.DTOs.Roles;

namespace CompVault.Frontend.Features.Roles.Models;

/// <summary>
/// Modellen for å tilknytte en eller flere permissions til en rolle
/// </summary>
public class AssignPermissionsModel
{
    /// <summary>
    /// Valgte permissions navn
    /// </summary>
    public IList<string> SelectedPermissionNames { get; set; } = [];

    public AssignPermissionsRequest ToRequest() => new() { PermissionNames = SelectedPermissionNames };

    public static AssignPermissionsModel FromDto(RoleDto dto) => new()
    {
        SelectedPermissionNames = dto.Permissions.ToList()
    };
    
}