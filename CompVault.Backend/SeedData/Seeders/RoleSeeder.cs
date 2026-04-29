using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        foreach ((string name, string description) in BarnehageData.Roles)
        {
            if (await roleManager.RoleExistsAsync(name))
                continue;

            ApplicationRole role = new()
            {
                Name = name,
                Description = description,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow,
            };

            IdentityResult result = await roleManager.CreateAsync(role);
            if (result.Succeeded)
                logger.LogDebug("[Seeder] Rolle opprettet: {Role}", name);
            else
                logger.LogWarning("[Seeder] Feil ved opprettelse av rolle {Role}: {Errors}",
                    name, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
