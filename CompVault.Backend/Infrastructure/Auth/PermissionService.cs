using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Implementerer <see cref="IPermissionService"/>.
/// </summary>
public sealed class PermissionService(
    RoleManager<ApplicationRole> roleManager,
    AppDbContext dbContext) : IPermissionService
{
    /// <inheritdoc />
    public async Task<List<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken ct)
    {
        if (roleNames is null)
        {
            return [];
        }

        var roleNameSet = new HashSet<string>(
            roleNames.Where(r => r is not null),
            StringComparer.OrdinalIgnoreCase);

        Guid[] roleIds = await roleManager.Roles
            .Where(r => roleNameSet.Contains(r.Name!))
            .Select(r => r.Id)
            .ToArrayAsync(ct);

        return await dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(ct);
    }
}
