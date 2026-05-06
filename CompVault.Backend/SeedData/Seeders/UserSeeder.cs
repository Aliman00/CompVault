using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class UserSeeder
{
    /// <summary>
    /// Seeder brukere og kobler dem til avdeling, stilling og leder.
    /// </summary>
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ILogger logger,
        Dictionary<string, Guid> departmentIds)
    {
        // Bygg oppslag: Epost -> ManagerEpost
        var userManagerMap = BarnehageData.Users
            .Where(u => u.ManagerEmail is not null)
            .ToDictionary(u => u.Email, u => u.ManagerEmail, StringComparer.OrdinalIgnoreCase);

        // Opprett alle brukere først (uten ManagerId)
        var userIdMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach ((string firstName, string lastName, string email, string deptName, string jobTitleName, string? _, string[] roles, DateTime createdAt) in BarnehageData.Users)
        {
            bool exists = await dbContext.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email);
            if (exists)
            {
                ApplicationUser? existing = await dbContext.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
                userIdMap[email] = existing.Id;
                continue;
            }

            if (!departmentIds.TryGetValue(deptName, out Guid deptId))
            {
                logger.LogWarning("[Seeder] Avdeling ikke funnet for bruker {Email}: {Dept}", email, deptName);
                continue;
            }

            JobTitle? jobTitle = await dbContext.JobTitles
                .FirstOrDefaultAsync(jt => jt.Name == jobTitleName);
            if (jobTitle is null)
            {
                logger.LogWarning("[Seeder] Stillingstittel ikke funnet for bruker {Email}: {Title}", email, jobTitleName);
                continue;
            }

            ApplicationUser user = new()
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                EmploymentType = EmploymentType.Permanent,
                IsActive = true,
                CreatedAt = createdAt,
                DepartmentId = deptId,
                JobTitleId = jobTitle.Id,
            };

            // Passord trengs for Identity CreateAsync selv om vi bruker OTP.
            // Bruker et tilfeldig passord siden innlogging skjer via OTP.
            IdentityResult createResult = await userManager.CreateAsync(user, "TempPass123!");
            if (!createResult.Succeeded)
            {
                logger.LogWarning("[Seeder] Feil ved opprettelse av bruker {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            userIdMap[email] = user.Id;

            IdentityResult roleResult = await userManager.AddToRolesAsync(user, roles);
            if (roleResult.Succeeded)
                logger.LogDebug("[Seeder] Bruker opprettet: {Email} ({Roles})", email, string.Join(", ", roles));
            else
                logger.LogWarning("[Seeder] Feil ved tildeling av roller til {Email}: {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        // Oppdater ManagerId i andre omgang
        foreach ((string email, string? managerEmail) in userManagerMap)
        {
            if (managerEmail is null || !userIdMap.TryGetValue(email, out Guid userId))
                continue;

            if (!userIdMap.TryGetValue(managerEmail, out Guid managerId))
            {
                logger.LogWarning("[Seeder] Leder ikke funnet: {Manager} for {User}", managerEmail, email);
                continue;
            }

            ApplicationUser? user = await dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                continue;

            user.ManagerId = managerId;
            IdentityResult updateResult = await userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
                logger.LogDebug("[Seeder] Bruker {Email} koblet til leder {Manager}", email, managerEmail);
            else
                logger.LogWarning("[Seeder] Feil ved kobling av leder for {Email}: {Errors}",
                    email, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
        }
    }
}