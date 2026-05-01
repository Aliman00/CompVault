using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class DocumentCategorySeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach ((string documentTypeSlug, string name, string slug) in BarnehageData.DocumentCategories)
        {
            DocumentType? documentType = await dbContext.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Slug == documentTypeSlug);
            if (documentType is null)
            {
                logger.LogWarning("[Seeder] Dokumenttype ikke funnet for kategori: {Slug}", documentTypeSlug);
                continue;
            }

            bool exists = await dbContext.DocumentTypeCategories
                .AnyAsync(c => c.DocumentTypeId == documentType.Id && c.Slug == slug);
            if (exists)
                continue;

            DocumentTypeCategory category = new()
            {
                DocumentTypeId = documentType.Id,
                Name = name,
                Slug = slug,
                IsActive = true,
            };

            dbContext.DocumentTypeCategories.Add(category);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] Kategori opprettet: {Name} for {DocumentType}", name, documentTypeSlug);
        }
    }
}