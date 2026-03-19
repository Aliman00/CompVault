using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Tests.Common;
using CompVault.Tests.Common.Constants;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Tests.Backend.Integrations.Repositories;

public class OtpCodeRepositoryIntegrationsTests(
    BackendWebApplicationFactory factory) : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
{
    private AppDbContext _context = null!;
    private OtpCodeRepository _sut = null!;
    
    public async Task InitializeAsync()
    {
        await TestDataSeeder.CreateDb(factory.Services);
        await TestDataSeeder.SeedUserAsync(factory.Services, id: TestConstants.Users.ActiveUserId);
        
        // Oppretter scope for systemet vi tester - gjør det engang i konstruktøren for å slippe 
        // og gjenta dette i hver test
        var scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _sut = new OtpCodeRepository(_context);
    }

    public Task DisposeAsync() => Task.CompletedTask;
    
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
        var otpCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services, userId: TestConstants.Users.ActiveUserId);

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

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
        var newestCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId);
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId,  createdAt: DateTime.UtcNow.AddMinutes(-5));

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

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
        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

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
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId,  expiresAt: DateTime.UtcNow.AddMinutes(-1));

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

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
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId,  isUsed: true);

        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

        // Assert
        existingOtpCode.Should().BeNull();
    }

    /// <summary>
    /// Tester at vi ikke henter en aktiv OtpCode for en annen bruker
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_WrongUserId_ReturnsNull()
    {
        // Arrange - seeder en Otp-kode til bruker A vi har opprettet i kosntruktøren
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services, userId: TestConstants.Users.ActiveUserId);
        
        var userWithoutCode = await TestDataSeeder.SeedUserAsync(factory.Services, email: "userb@compvault.com");
        
        // Act - Kaller metoden med en annen brukerId
        var existingOtpCode = await _sut.GetActiveCodeAsync(userWithoutCode.Id);

        // Assert
        existingOtpCode.Should().BeNull();
    }
}