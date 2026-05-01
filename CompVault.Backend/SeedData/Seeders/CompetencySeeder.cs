using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Competencies;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class CompetencySeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ILogger logger)
    {
        DateTime today = DateTime.UtcNow.Date;

        foreach ((string email, string typeName, int issuedOffsetDays, int? expiryOffsetDays, string? certNumber) in BarnehageData.Competencies)
        {
            ApplicationUser? user = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                logger.LogWarning("[Seeder] Bruker ikke funnet: {Email}", email);
                continue;
            }

            CompetencyType? ct = await dbContext.CompetencyTypes.FirstOrDefaultAsync(c => c.Name == typeName);
            if (ct is null)
            {
                logger.LogWarning("[Seeder] Kompetansetype ikke funnet: {Type}", typeName);
                continue;
            }

            bool exists = await dbContext.Competencies
                .IgnoreQueryFilters()
                .AnyAsync(c => c.UserId == user.Id && c.CompetencyTypeId == ct.Id);
            if (exists)
                continue;

            DateTime issuedDate = today.AddDays(issuedOffsetDays);
            DateTime? expiryDate = expiryOffsetDays.HasValue
                ? today.AddDays(expiryOffsetDays.Value)
                : null;
            CompetencyStatus status = CompetencyStatusCalculator.Calculate(expiryDate);

            Competency competency = new()
            {
                UserId = user.Id,
                CompetencyTypeId = ct.Id,
                Status = status,
                IssuedDate = issuedDate,
                ExpiryDate = expiryDate,
                CertificateNumber = certNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            dbContext.Competencies.Add(competency);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] Kompetanse opprettet: {User} - {Type} ({Status})", email, typeName, status);
        }
    }
}