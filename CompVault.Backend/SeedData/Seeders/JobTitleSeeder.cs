using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class JobTitleSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach ((string name, bool isLeader) in BarnehageData.JobTitles)
        {
            bool exists = await dbContext.JobTitles.AnyAsync(jt => jt.Name == name);
            if (exists)
                continue;

            JobTitle jobTitle = new()
            {
                Name = name,
                IsLeader = isLeader,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            dbContext.JobTitles.Add(jobTitle);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] Stillingstittel opprettet: {Name} (IsLeader={IsLeader})", name, isLeader);
        }
    }
}