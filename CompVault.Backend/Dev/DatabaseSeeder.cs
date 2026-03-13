using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace CompVault.Backend.Dev;

/// <summary>
/// Seeder databasen med testdata for Development-miljøet.
/// ADVARSEL: Skal KUN kjøres i Development — fjern Dev/-mappen og seed-kallet i Program.cs før deploy.
/// </summary>
public static class DatabaseSeeder
{
    private const string DefaultPassword = "Test123!";

    private static readonly (string FirstName, string LastName, string Email, string JobTitle, string[] Roles)[] Users =
    [
        ("Kari",   "Nordmann", "kari.nordmann@compvault.no", "System Administrator", ["Admin"]),
        ("Ola",    "Nordmann", "ola.nordmann@compvault.no",  "IT-leder",             ["Admin"]),
        ("Lars",   "Hansen",   "lars.hansen@compvault.no",   "Systemutvikler",       ["Employee"]),
        ("Ingrid", "Berg",     "ingrid.berg@compvault.no",   "Systemutvikler",       ["Employee"]),
        ("Tobias", "Lie",      "tobias.lie@compvault.no",    "Rådgiver",             ["Employee"]),
        ("Sofie",  "Dahl",     "sofie.dahl@compvault.no",    "HR-konsulent",         ["Employee"]),
    ];

    private static readonly (string Name, string Description)[] Roles =
    [
        ("Admin",    "Full tilgang til alle funksjoner i systemet."),
        ("Employee", "Standard ansatt-tilgang."),
    ];

    /// <summary>
    /// Kjør seed. Oppretter roller og brukere dersom de ikke allerede finnes.
    /// </summary>
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        logger.LogInformation("[DatabaseSeeder] Starter seeding av testdata...");

        await SeedRolesAsync(roleManager, logger);
        await SeedUsersAsync(userManager, logger);

        logger.LogInformation("[DatabaseSeeder] Seeding fullført.");
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        foreach ((string name, string description) in Roles)
        {
            if (await roleManager.RoleExistsAsync(name))
                continue;

            ApplicationRole role = new()
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
            };

            IdentityResult result = await roleManager.CreateAsync(role);
            if (result.Succeeded)
                logger.LogInformation("[DatabaseSeeder] Rolle opprettet: {Role}", name);
            else
                logger.LogWarning("[DatabaseSeeder] Feil ved opprettelse av rolle {Role}: {Errors}",
                    name, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        foreach ((string firstName, string lastName, string email, string jobTitle, string[] roles) in Users)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            ApplicationUser user = new()
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                JobTitle = jobTitle,
                EmploymentType = EmploymentType.Permanent,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            IdentityResult createResult = await userManager.CreateAsync(user, DefaultPassword);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("[DatabaseSeeder] Feil ved opprettelse av bruker {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            IdentityResult roleResult = await userManager.AddToRolesAsync(user, roles);
            if (roleResult.Succeeded)
                logger.LogInformation("[DatabaseSeeder] Bruker opprettet: {Email} ({Roles})",
                    email, string.Join(", ", roles));
            else
                logger.LogWarning("[DatabaseSeeder] Feil ved tildeling av roller til {Email}: {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}
