using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Constants;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class RolePermissionSeeder
{
    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager, AppDbContext dbContext, ILogger logger)
    {
        List<Permission> allPermissions = await dbContext.Permissions.ToListAsync();

        // ===================== Admin =====================
        await SeedRolePermissionsAsync(roleManager, dbContext, allPermissions, "Admin", allPermissions.Select(p => p.Name).ToList(), logger);

        // ===================== Avdelingsleder =====================
        // Leder for hovedavdeling: se egen + underavdelinger. Kan styre brukere, kompetanser, dokumenter, utstyr.
        string[] avdelingsLederPerms =
        [
            Permissions.UsersRead, Permissions.UsersReadSub, Permissions.UsersWrite, Permissions.UsersDelete,
            Permissions.DepartmentsRead, Permissions.DepartmentsReadSub,
            Permissions.CompetenciesRead, Permissions.CompetenciesReadSub, Permissions.CompetenciesWrite,
            Permissions.DocumentsRead, Permissions.DocumentsReadSub, Permissions.DocumentsWrite, Permissions.DocumentsSign,
            Permissions.EquipmentRead, Permissions.EquipmentReadSub, Permissions.EquipmentWrite,
            Permissions.JobTitlesRead, Permissions.DocumentTypesRead,
        ];
        await SeedRolePermissionsAsync(roleManager, dbContext, allPermissions, "Avdelingsleder", avdelingsLederPerms.ToList(), logger);

        // ===================== Gruppeleder =====================
        // Leder for underavdeling: se kun egen avdeling. Kan styre ansatte og dagen.
        string[] gruppeLederPerms =
        [
            Permissions.UsersRead,
            Permissions.DepartmentsRead,
            Permissions.CompetenciesRead, Permissions.CompetenciesWrite,
            Permissions.DocumentsRead, Permissions.DocumentsWrite, Permissions.DocumentsSign,
            Permissions.EquipmentRead, Permissions.EquipmentWrite,
            Permissions.JobTitlesRead, Permissions.DocumentTypesRead,
        ];
        await SeedRolePermissionsAsync(roleManager, dbContext, allPermissions, "Gruppeleder", gruppeLederPerms.ToList(), logger);

        // ===================== Ansatt =====================
        // Vanlig ansatt: egen data. Kan IKKE se andre brukere. GET /api/auth/me erstatter profil-oppslag.
        string[] ansattPerms =
        [
            Permissions.CompetenciesRead,
            Permissions.DocumentsRead, Permissions.DocumentsSign,
            Permissions.EquipmentRead,
            Permissions.JobTitlesRead, Permissions.DocumentTypesRead,
        ];
        await SeedRolePermissionsAsync(roleManager, dbContext, allPermissions, "Ansatt", ansattPerms.ToList(), logger);
    }

    private static async Task SeedRolePermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        AppDbContext dbContext,
        List<Permission> allPermissions,
        string roleName,
        List<string> permissionNames,
        ILogger logger)
    {
        ApplicationRole? role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            logger.LogWarning("[Seeder] Rolle ikke funnet: {Role}", roleName);
            return;
        }

        var permissions = allPermissions.Where(p => permissionNames.Contains(p.Name)).ToList();
        int addedCount = 0;

        foreach (Permission permission in permissions)
        {
            bool exists = await dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
            if (exists)
                continue;

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                GrantedAt = DateTime.UtcNow,
            });
            addedCount++;
        }

        if (addedCount > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] {Role} tildelt {Count} permissions", roleName, addedCount);
        }
    }
}
