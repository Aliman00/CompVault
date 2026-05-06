using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class CompetencyTypeSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach ((string name, string? description, string? category, bool requiresExpiration) in BarnehageData.CompetencyTypes)
        {
            bool exists = await dbContext.CompetencyTypes.AnyAsync(ct => ct.Name == name);
            if (exists)
                continue;

            CompetencyType ct = new()
            {
                Name = name,
                Description = description,
                Category = category,
                RequiresExpiration = requiresExpiration,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            dbContext.CompetencyTypes.Add(ct);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] Kompetansetype opprettet: {Name}", name);
        }
    }
}