using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Enums;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class DocumentTypeSeeder
{
    private static readonly string[] DefaultAllowedMimeTypes =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        ApplicationUser? admin = await dbContext.Users
            .IgnoreQueryFilters()
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        foreach ((string name, string slug, string? description, DocumentTargetMode targetMode) in BarnehageData.DocumentTypes)
        {
            bool exists = await dbContext.DocumentTypes.AnyAsync(dt => dt.Slug == slug);
            if (exists)
                continue;

            DocumentType documentType = new()
            {
                Name = name,
                Slug = slug,
                Description = description,
                TargetMode = targetMode,
                StorageFolder = slug,
                AllowedMimeTypes = DefaultAllowedMimeTypes,
                MaxFileSizeBytes = 20 * 1024 * 1024,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedById = admin?.Id,
            };

            dbContext.DocumentTypes.Add(documentType);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] Dokumenttype opprettet: {Name} ({Slug})", name, slug);
        }
    }
}