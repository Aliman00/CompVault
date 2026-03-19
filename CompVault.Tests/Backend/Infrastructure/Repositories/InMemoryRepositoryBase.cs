using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Tests.Common;
using CompVault.Tests.Common.Constants;
using Microsoft.EntityFrameworkCore;
namespace CompVault.Tests.Backend.Infrastructure.Repositories;

/// <summary>
/// Baseklasse for repoet. Slipper å sette opp InMemoryDatabase manuelt på hver eneste repository-test
/// </summary>
public abstract class InMemoryRepositoryBase : IDisposable
{
    protected readonly AppDbContext Context;

    protected InMemoryRepositoryBase()
    {
        // Setter opp InMemoryDatabase
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        Context = new AppDbContext(options);
    }
    
    /// <summary>
    /// Seeder en bruker og lagrer den i InMemory-databasen
    /// </summary>
    /// <param name="email">Default epost, eller en annen epost hvis det er en annen bruker</param>
    /// <returns>Den opprettede brukeren hvis egenskaper (som ID) er nødvendig for testing</returns>
    protected async Task<ApplicationUser> SeedUserAsync(string email = TestConstants.Users.DefaultEmailForActiveUser)
    {
        var user = TestDataFactory.CreateApplicationUser(email: email);
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }
    
    /// <summary>
    /// Hjelpemetode for å seede en OtpCodeAsync i databasen for en bruker. Den har default verdier til en
    /// ikke-eksisterende OtpCode, men parameter for å endre koden
    /// </summary>
    /// <param name="userId">Bruker ID til brukeren som får en OtpCode</param>
    /// <param name="isUsed">Koden er enten used eller ikke</param>
    /// <param name="expiresAt">Koden har enten en utgåttende tid eller ingen</param>
    /// <param name="createdAt">Koden har en opprettet tid hvis den eksisterer, eller så har den ikke det</param>
    /// <returns>En eksisterende OtpCode</returns>
    protected async Task<OtpCode> SeedOtpCodeAsync(Guid userId, bool isUsed = false,
        DateTime? expiresAt = null, DateTime? createdAt = null)
    {
        var otpCode = TestDataFactory.CreateOtpCode(userId: userId, isUsed: isUsed,
            expiresAt: expiresAt, createdAt: createdAt);

        Context.Set<OtpCode>().Add(otpCode);
        await Context.SaveChangesAsync();
        return otpCode;
    }
    
    /// <summary>
    /// Hjelpemetode for å seede en RefreshToken inn i databasen. Default verdier, men kan endres hvis trengs
    /// </summary>
    /// <param name="userId">Brukeren som Token tilhører</param>
    /// <param name="token">Selve token, kun en enkel string i testene</param>
    /// <param name="createdAt">Når den er opprettet. Default UtcNow</param>
    /// <param name="expiresAt">Når den utgår. Default om 15 min fra opprettelse</param>
    /// <param name="isRevoked">Bool på om koden er gyldig eller revoked</param>
    /// <returns>RefreshToken, som er lagt til i databasen</returns>
    protected async Task<RefreshToken> SeedRefreshTokenAsync(Guid? userId = null, 
        string token = TestConstants.RefreshToken.Token, DateTime? createdAt = null, DateTime? expiresAt = null, 
        bool isRevoked = false)
    {
        var refreshToken = TestDataFactory.CreateRefreshToken(userId, token, createdAt, expiresAt, isRevoked);

        Context.Set<RefreshToken>().Add(refreshToken);
        await Context.SaveChangesAsync();
        return refreshToken;
    }
    
    public void Dispose() => Context.Dispose();
}