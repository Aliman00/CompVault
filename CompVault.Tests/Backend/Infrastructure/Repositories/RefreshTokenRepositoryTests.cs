using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Tests.Backend.Infrastructure.Repositories;

public class RefreshTokenRepositoryTests
{
    // Mocker AppDbContext og setter opp systemet for testing
    private readonly AppDbContext _context;
    private readonly RefreshTokenRepository _sut;

    public RefreshTokenRepositoryTests()
    {
        // Setter opp InMemoryDatabase
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new RefreshTokenRepository(_context);
    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------

    /// <summary>
    /// Hjelpemetode for å seede en OtpCodeAsync i databasen for en bruker. Den har default verdier til en
    /// ikke-eksisterende OtpCode, men parameter for å endre koden
    /// </summary>
    /// <param name="userId">Bruker ID til brukeren som får en OtpCode</param>
    /// <param name="isUsed">Koden er enten used eller ikke</param>
    /// <param name="expiresAt">Koden har enten en utgåttende tid eller ingen</param>
    /// <param name="createdAt">Koden har en opprettet tid hvis den eksisterer, eller så har den ikke det</param>
    /// <returns>En eksisterende OtpCode</returns>
    private async Task<OtpCode> SeedOtpCodeAsync(Guid userId, bool isUsed = false,
        DateTime? expiresAt = null, DateTime? createdAt = null)
    {
        var otpCode = TestDataSeeder.CreateOtpCode(userId: userId, isUsed: isUsed,
            expiresAt: expiresAt, createdAt: createdAt);

        _context.Set<OtpCode>().Add(otpCode);
        await _context.SaveChangesAsync();
        return otpCode;
    }

    /// <summary>
    /// Seeder en bruker og lagrer den i InMemory-databasen
    /// </summary>
    /// <param name="email">Default epost, eller en annen epost hvis det er en annen bruker</param>
    /// <returns>Den opprettede brukeren hvis egenskaper (som ID) er nødvendig for testing</returns>
    private async Task<ApplicationUser> SeedUserAsync(string email = "test@example.com")
    {
        var user = TestDataSeeder.CreateApplicationUser(email: email);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}