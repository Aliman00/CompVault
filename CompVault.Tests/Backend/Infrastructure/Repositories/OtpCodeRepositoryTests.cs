using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Tests.Common;
using CompVault.Tests.Common.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Tests.Backend.Infrastructure.Repositories;

public class OtpCodeRepositoryTests : IDisposable
{
    // Mocker AppDbContext og setter opp systemet for testing
    private readonly AppDbContext _context;
    private readonly OtpCodeRepository _sut;

    public OtpCodeRepositoryTests()
    {
        // Setter opp InMemoryDatabase
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new OtpCodeRepository(_context);
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
    private async Task<ApplicationUser> SeedUserAsync(string email = TestConstants.Users.DefaultEmailForActiveUser)
    {
        var user = TestDataSeeder.CreateApplicationUser(email: email);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    // -------------------------------------------------------------------------
    // GetActiveCodeAsync - Finner eksisterende kode
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at vi finner et aktivt OtpCode-objekt i databasen
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_ActiveUnexpiredCode_ReturnsCode()
    {
        // Arrange - seeder en default kode
        var user = await SeedUserAsync();
        var otpCode = await SeedOtpCodeAsync(user.Id);

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(user.Id);

        // Assert
        existingOtpCode.Should().NotBeNull();
        existingOtpCode.Id.Should().Be(otpCode.Id);
    }

    /// <summary>
    /// Tester at det metoden henter sist opprettet aktive OtpCode i databasen, hvis det eksisterer flere.
    /// Vi har et SQL-filter og logikk i servicen for å sikre at dette ikke vil skje i produksjon.
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_MultipleActiveUnexpiredCode_ReturnsNewestCode()
    {
        // Arrange - seeder 2 stk koder med forskjellig tid
        var user = await SeedUserAsync();
        var newestCode = await SeedOtpCodeAsync(user.Id, createdAt: DateTime.UtcNow);
        await SeedOtpCodeAsync(user.Id, createdAt: DateTime.UtcNow.AddMinutes(-5));

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(user.Id);

        // Assert
        existingOtpCode.Should().NotBeNull();
        existingOtpCode.Id.Should().Be(newestCode.Id);
    }

    // -------------------------------------------------------------------------
    // GetActiveCodeAsync - Finner ingen eksisterende kode
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at brukeren ikke har noen eksisterende koder i databasen
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_NoExistingCode_ReturnsNull()
    {
        // Arrange - Seeder en bruker
        var user = await SeedUserAsync();

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(user.Id);

        // Assert
        existingOtpCode.Should().BeNull();
    }

    /// <summary>
    /// Tester at metoden filterer bort utgåtte Otp-koder
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_CodeIsExpired_ReturnsNull()
    {
        // Arrange - seeder en Otp-kode som er utgått for 1 minutt siden
        var user = await SeedUserAsync();
        await SeedOtpCodeAsync(user.Id, expiresAt: DateTime.UtcNow.AddMinutes(-1));

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(user.Id);

        // Assert
        existingOtpCode.Should().BeNull();
    }

    /// <summary>
    /// Sjekker at metoden filterer bort brukte koder
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_CodeIsUsed_ReturnsNull()
    {
        // Arrange - seeder en Otp-kode som er utgått for 1 minutt siden
        var user = await SeedUserAsync();
        await SeedOtpCodeAsync(user.Id, isUsed: true);

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(user.Id);

        // Assert
        existingOtpCode.Should().BeNull();
    }

    /// <summary>
    /// Tester at vi ikke henter en aktiv OtpCode for en annen bruker
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_WrongUserId_ReturnsNull()
    {
        // Arrange - seeder en Otp-kode til bruker A
        var userWithCode = await SeedUserAsync();
        await SeedOtpCodeAsync(userWithCode.Id);

        // Oppretter bruker B uten kode
        var userWithoutCode = await SeedUserAsync("test123@example.com");

        // Act - Kaller metoden med en annen brukerId
        var existingOtpCode = await _sut.GetActiveCodeAsync(userWithoutCode.Id);

        // Assert
        existingOtpCode.Should().BeNull();
    }


    public void Dispose() => _context.Dispose();

}
