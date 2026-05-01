using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class DocumentSignatureSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        if (BarnehageData.DocumentSignatures.Length == 0)
        {
            logger.LogDebug("[Seeder] Ingen forhåndsdefinerte signaturer å seede.");
            return;
        }

        int totalSignatures = 0;

        foreach ((string documentTitle, string userEmail, DateTime signedAt) in BarnehageData.DocumentSignatures)
        {
            Document? document = await dbContext.Documents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Title == documentTitle && d.DeletedAt == null);
            if (document is null)
            {
                logger.LogWarning("[Seeder] Dokument ikke funnet for signatur: {Title}", documentTitle);
                continue;
            }

            ApplicationUser? user = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user is null)
            {
                logger.LogWarning("[Seeder] Bruker ikke funnet for signatur: {Email}", userEmail);
                continue;
            }

            bool alreadySigned = await dbContext.DocumentSignatures
                .IgnoreQueryFilters()
                .AnyAsync(s => s.DocumentId == document.Id && s.UserId == user.Id);
            if (alreadySigned)
                continue;

            var signature = new DocumentSignature
            {
                DocumentId = document.Id,
                UserId = user.Id,
                SignedAt = signedAt,
                SignatureVersion = document.Version,
            };

            dbContext.DocumentSignatures.Add(signature);
            await dbContext.SaveChangesAsync();
            totalSignatures++;
            logger.LogDebug("[Seeder] Signatur: {User} signerte '{Title}' {Date}",
                userEmail, documentTitle, signedAt.ToString("yyyy-MM-dd"));
        }

        logger.LogDebug("[Seeder] {Count} signaturer opprettet totalt.", totalSignatures);
    }
}