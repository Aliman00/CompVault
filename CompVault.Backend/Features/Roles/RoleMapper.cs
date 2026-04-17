using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.DTOs.Roles;

namespace CompVault.Backend.Features.Roles;

/// <summary>
/// Mapper for konvertering mellom <see cref="ApplicationRole"/> og <see cref="RoleDto"/>.
/// </summary>
public static class RoleMapper
{
    /// <summary>
    /// Konverterer en <see cref="ApplicationRole"/> til en <see cref="RoleDto"/>.
    /// </summary>
    public static RoleDto ToDto(ApplicationRole role, int userCount, IReadOnlyList<string> permissionNames) => new()
    {
        Id = role.Id,
        Name = role.Name ?? string.Empty,
        Description = role.Description,
        UserCount = userCount,
        CreatedAt = role.CreatedAt,
        CreatedById = role.CreatedById,
        IsSystem = role.IsSystem,
        CreatedByName = role.CreatedBy != null 
            ? $"{role.CreatedBy.FirstName} {role.CreatedBy.LastName}" 
            : null,
        Permissions = permissionNames
    };

    /// <summary>
    /// Konverterer en <see cref="Permission"/> til en <see cref="ExpiringCompetencyDto"/>.
    /// </summary>
    public static ExpiringCompetencyDto ToPermissionDto(Permission permission) => new()
    {
        Name = permission.Name,
        Description = permission.Description,
        Category = permission.Category
    };
}