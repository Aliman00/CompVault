using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.SeedData.Seeders;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;

namespace CompVault.Backend.SeedData;

/// <summary>
/// Seeder databasen med testdata for Development-miljøet.
/// Kjøres automatisk ved oppstart når ASPNETCORE_ENVIRONMENT=Development.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Kjør seed i riktig rekkefølge innenfor én transaksjon.
    /// </summary>
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AppDbContext dbContext,
        ILogger logger)
    {
        logger.LogInformation("[DatabaseSeeder] Starter seeding av testdata...");

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            await RoleSeeder.SeedAsync(roleManager, logger);
            await PermissionSeeder.SeedAsync(dbContext, logger);
            await RolePermissionSeeder.SeedAsync(roleManager, dbContext, logger);
            await JobTitleSeeder.SeedAsync(dbContext, logger);
            Dictionary<string, Guid> departmentIds = await DepartmentSeeder.SeedAsync(dbContext, logger);
            await UserSeeder.SeedAsync(userManager, dbContext, logger, departmentIds);
            await CompetencyTypeSeeder.SeedAsync(dbContext, logger);
            await CompetencySeeder.SeedAsync(dbContext, logger);
            await DocumentTypeSeeder.SeedAsync(dbContext, logger);
            await DocumentCategorySeeder.SeedAsync(dbContext, logger);
            await DocumentSeeder.SeedAsync(dbContext, logger);
            await DocumentSignatureSeeder.SeedAsync(dbContext, logger);
            await EquipmentSeeder.SeedAsync(dbContext, logger);

            await transaction.CommitAsync();
            logger.LogInformation("[DatabaseSeeder] Seeding fullført.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "[DatabaseSeeder] Feil under seeding – transaksjon rullet tilbake");
            throw;
        }
    }
}