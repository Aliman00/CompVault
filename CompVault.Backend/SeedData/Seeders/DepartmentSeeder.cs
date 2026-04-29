using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class DepartmentSeeder
{
    /// <summary>
    /// Seeder avdelinger. Returnerer dictionary med navn -> Id.
    /// </summary>
    public static async Task<Dictionary<string, Guid>> SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        var departmentIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        ApplicationUser? admin = await dbContext.Users
            .IgnoreQueryFilters()
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        // Opprett toppnivå først
        foreach ((string name, string description, string? parentName) in BarnehageData.Departments)
        {
            if (parentName is not null)
                continue;

            bool exists = await dbContext.Departments.IgnoreQueryFilters().AnyAsync(d => d.Name == name);
            if (exists)
            {
                Department? existing = await dbContext.Departments.IgnoreQueryFilters().FirstAsync(d => d.Name == name);
                departmentIds[name] = existing.Id;
                continue;
            }

            Department dept = new()
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedById = admin?.Id,
            };

            dbContext.Departments.Add(dept);
            await dbContext.SaveChangesAsync();
            departmentIds[name] = dept.Id;
            logger.LogDebug("[Seeder] Avdeling opprettet: {Name}", name);
        }

        // Opprett underavdelinger
        foreach ((string name, string description, string? parentName) in BarnehageData.Departments)
        {
            if (parentName is null)
                continue;

            bool exists = await dbContext.Departments.IgnoreQueryFilters().AnyAsync(d => d.Name == name);
            if (exists)
            {
                Department? existing = await dbContext.Departments.IgnoreQueryFilters().FirstAsync(d => d.Name == name);
                departmentIds[name] = existing.Id;
                continue;
            }

            if (!departmentIds.TryGetValue(parentName, out Guid parentId))
            {
                logger.LogWarning("[Seeder] Kunne ikke finne parent-avdeling {Parent} for {Name}", parentName, name);
                continue;
            }

            Department dept = new()
            {
                Name = name,
                Description = description,
                ParentDepartmentId = parentId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedById = admin?.Id,
            };

            dbContext.Departments.Add(dept);
            await dbContext.SaveChangesAsync();
            departmentIds[name] = dept.Id;
            logger.LogDebug("[Seeder] Avdeling opprettet: {Name} (under {Parent})", name, parentName);
        }

        return departmentIds;
    }
}
