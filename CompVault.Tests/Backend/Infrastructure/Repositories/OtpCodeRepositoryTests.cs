using CompVault.Backend.Infrastructure.Repositories.Auth;
using FluentAssertions;

namespace CompVault.Tests.Backend.Infrastructure.Repositories;

public class OtpCodeRepositoryTests : InMemoryRepositoryBase
{
    private readonly OtpCodeRepository _sut;

    public OtpCodeRepositoryTests()
    {
        _sut = new OtpCodeRepository(Context);
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

}
