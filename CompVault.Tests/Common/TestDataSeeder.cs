using CompVault.Backend.Common.Security;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Tests.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Tests.Common;

/// <summary>
/// Oppretter ApplicationUsers for testing, og seeder inne i InMemory-databaser
/// </summary>
public static class TestDataSeeder
{
    // -------------------------------------------------------------------------
    // Database
    // -------------------------------------------------------------------------
    /// <summary>
    /// Sletter en eksisterende database, og oppretter en ny database mellom hver integrasjonstest
    /// Legger til en aktiv og en inaktiv bruker ved oppstart
    /// Trenger som regel alltid en bruker, men kan hende det er best å dele denne opp i en metode for å opprette
    /// databasen og en for å opprette brukere
    /// </summary>
    public static async Task CreateDb(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Nuker databasen og oppretter en ny database for hver integrasjonstest
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync(); // TODO: Bytt til MigrateAsync når vi har migrasjon
    }

    // -------------------------------------------------------------------------
    // Users
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oppretter en ApplicationUser for testing. Brukes i de fleste testene.
    /// Hvis deletedAt har en verdi, så er brukeren inaktive/slettet
    /// Guid er optional. Bruker ActiveUserId som default hvis ingen annen informasjon er oppgitt
    /// </summary>
    /// <param name="id">ID til en bruker hvis man trenger å slå opp ID for testing</param>
    /// <param name="email">Optional string med Epost for å opprette forskjellige brukere</param>
    /// <param name="deletedAt">DateTime som bestemmer om brukeren er aktive/slettet</param>
    /// <returns>En ferdig opprettet ApplicationUser for testing</returns>
    public static ApplicationUser CreateApplicationUser(Guid? id = null,
        string email = TestConstants.Users.DefaultEmailForActiveUser, DateTime? deletedAt = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = email,
        UserName = email,
        FirstName = "Fredrik",
        LastName = "Magee",
        IsActive = deletedAt == null,
        DeletedAt = deletedAt
    };
    
    /// <summary>
    /// Oppretter en Otp-kode tilhørende en bruker
    /// </summary>
    /// <param name="userId">Brukeren som Otp-koden tilhører</param>
    /// <param name="plainTextCode">Koden i plaintext som blir hashet i metoden</param>
    /// <param name="expiresAtMin">Antall minutter til den utgår</param>
    /// <param name="failedAttempts">Antall feilede forsøk</param>
    /// <returns>En opprettet OtpCode</returns>
    public static OtpCode CreateOtpCode(Guid? userId = null, string plainTextCode = TestConstants.Otp.PlainTextOtpCode, 
        int expiresAtMin = 10, int failedAttempts = 0) => new OtpCode
    {
        UserId = userId ?? TestConstants.Users.ActiveUserId,
        Code = OtpHasher.HashCode(plainTextCode),
        ExpiresAt = DateTime.UtcNow.AddMinutes(expiresAtMin),
        FailedAttempts = failedAttempts
    };


    /// <summary>
    /// Oppretter og seeder en bruker i databas med en rolle
    /// Kaller CreateApplicationUser som en wrapper som lagrer med context
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="id">ID til en bruker hvis man trenger å slå opp ID for testing</param>
    /// <param name="email">Optional string med Epost for å opprette forskjellige brukere</param>
    /// <param name="deletedAt">DateTime som bestemmer om brukeren er aktive/slettet</param>
    /// <param name="role"></param>
    /// <returns>En opprettet ApplicationUser som er seedet i databasen</returns>
    public static async Task<ApplicationUser> SeedUserAsync(IServiceProvider serviceProvider, Guid? id = null,
        string email = TestConstants.Users.DefaultEmailForActiveUser, DateTime? deletedAt = null,
        string role = TestConstants.Roles.Default)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Opprett rollen hvis den ikke eksisterer
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new ApplicationRole { Name = role });

        var user = CreateApplicationUser(id, email, deletedAt);
        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    // -------------------------------------------------------------------------
    // OTP
    // -------------------------------------------------------------------------

    /// <summary>
    /// Seeder en Otp-kode inn i databasen. Har optional felt på plainTextcode og failedAttempts hvis
    /// vi ønsker å 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="plainTextCode"></param>
    /// <param name="failedAttempts"></param>
    public static async Task SeedOtpCodeAsync(IServiceProvider serviceProvider,
        string plainTextCode = TestConstants.Otp.PlainTextOtpCode, int failedAttempts = 0)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Set<OtpCode>().Add(CreateOtpCode(userId: TestConstants.Users.ActiveUserId,
            plainTextCode: plainTextCode, failedAttempts: failedAttempts));

        await context.SaveChangesAsync();
    }
}
