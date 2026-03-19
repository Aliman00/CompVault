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

    // protected async Task<RefreshToken> SeedRefreshTokenAsync(Guid userId, )
    // {
    //     var refreshToken = TestDataFactory.CreateRefreshToken(userId: userId, );
    //
    // }
    
    public void Dispose() => Context.Dispose();
}