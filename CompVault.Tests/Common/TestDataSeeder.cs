using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Tests.Common.Constants;
using Microsoft.AspNetCore.Identity;
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

        var user = TestDataFactory.CreateApplicationUser(id, email, deletedAt);
        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    // -------------------------------------------------------------------------
    // OTP
    // -------------------------------------------------------------------------


    /// <summary>
    /// Seeder en Otp-kode inn i databasen. Har optional felt på alle egenskapene som er verdt å teste
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="userId">Brukeren som Otp-koden tilhører. Default til ActiveUserId</param>
    /// <param name="plainTextCode">Koden i plaintext som blir hashet i metoden. Default konstant</param>
    /// <param name="createdAt">Når OTP-koden er opprettet. Defauklt UtcNop</param>
    /// <param name="expiresAt">DateTime-objekt som spesifiserer når den går ut. Default om 10 min</param>
    /// <param name="failedAttempts">Antall feilede forsøk. Default = 0</param>
    /// <param name="isUsed">Setter om OTP-koden er brukt eller ikke. Default = false</param>
    /// <returns>Opprettet OtpCode</returns>
    public static async Task<OtpCode> SeedOtpCodeAsync(IServiceProvider serviceProvider, Guid? userId = null,
        string plainTextCode = TestConstants.Otp.PlainTextOtpCode, DateTime? createdAt = null, 
        DateTime? expiresAt = null, int failedAttempts = 0, bool isUsed = false)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var otpCode = TestDataFactory.CreateOtpCode(userId: userId,
            plainTextCode: plainTextCode, createdAt: createdAt, expiresAt: expiresAt, failedAttempts: failedAttempts, 
            isUsed: isUsed);

        context.Set<OtpCode>().Add(otpCode);
        await context.SaveChangesAsync();
        
        return otpCode;
    }
    
    /// <summary>
    /// Oppretter og seeder en RefreshToken inne i databasen
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="userId">Brukeren som Token tilhører. Default ActiveUserId</param>
    /// <param name="token">Selve token, kun en enkel string i testene. Default token-konstant</param>
    /// <param name="createdAt">Når den er opprettet. Default UtcNow</param>
    /// <param name="expiresAt">Når den utgår. Default om 15 min fra opprettelse</param>
    /// <param name="isRevoked">Bool på om koden er gyldig eller revoked</param>
    /// <returns>En opprettet RefreshToken</returns>
    public static async Task<RefreshToken> SeedRefreshTokenAsync(IServiceProvider serviceProvider,
        Guid? userId = null, string? token = null,
        DateTime? createdAt = null, DateTime? expiresAt = null, bool isRevoked = false)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var refreshToken = TestDataFactory.CreateRefreshToken(
            userId: userId ?? TestConstants.Users.ActiveUserId,
            token: token,
            createdAt: createdAt,
            expiresAt: expiresAt,
            isRevoked: isRevoked);
        
        context.Set<RefreshToken>().Add(refreshToken);
        await context.SaveChangesAsync();
        return refreshToken;
    }
}
