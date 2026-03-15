using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Tests.Backend.Infrastructure.Repositories;

public class OtpCodeRepositoryTests : IDisposable
{
    // Mocker AppDbContext og setter opp systemet for testing
    private readonly AppDbContext _context;
    private readonly OtpCodeRepository _sut;
    
    // Oppretter en UserId for testing
    private readonly Guid _userId = Guid.NewGuid();

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
    /// Hjelpemetode for å seede en OtpCodeAsync i databasen. Den har default verdier til en ikke-eksisterende
    /// OtpCode, men parameter for å endre koden
    /// </summary>
    /// <param name="userId">Bruker ID kan enten være en Guid eller null</param>
    /// <param name="isUsed">Koden er enten used eller ikke</param>
    /// <param name="expiresAt">Koden har enten en utgåttende tid eller ingen</param>
    /// <param name="createdAt">Koden har en opprettet tid hvis den eksisterer, eller så har den ikke det</param>
    /// <returns>En eksisterende OtpCode</returns>
    private async Task<OtpCode> SeedOtpCodeAsync(Guid? userId = null, bool isUsed = false, 
        DateTime? expiresAt = null, DateTime? createdAt = null)
    {
        var id = userId ?? _userId;
        
        // OtpCode krever i options at den tilhørerer en bruker, så vi må seede en bruker. Sikrer at vi kun
        // seeder en ikke-duplikat bruker pr runtime
        if (!_context.Users.Any(u => u.Id == id))
            await SeedUserAsync(id);
        
        var otpCode = new OtpCode
        {
            UserId = id,
            Code = "hashedcode",
            IsUsed = isUsed,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
            CreatedAt = createdAt ?? DateTime.UtcNow
        };

        _context.Set<OtpCode>().Add(otpCode);
        await _context.SaveChangesAsync();
        return otpCode;
    }
    
    /// <summary>
    /// Seeder en bruker og lagrer den i InMemory-databasen
    /// </summary>
    /// <param name="userId">Guid til brukerne</param>
    /// <param name="email">Default epost, eller en annen epost hvis det er en annen bruker</param>
    private async Task SeedUserAsync(Guid userId, string email = "test@example.com")
    {
        _context.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            DeletedAt = null
        });
        await _context.SaveChangesAsync();
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
        var seededCode = await SeedOtpCodeAsync();
        
        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(_userId);

        // Assert
        existingOtpCode.Should().NotBeNull();
        existingOtpCode.Id.Should().Be(seededCode.Id);
    }
    
    /// <summary>
    /// Tester at det metoden henter sist opprettet aktive OtpCode i databasen, hvis det eksisterer flere.
    /// Vi har et SQL-filter og logikk i servicen for å sikre at dette ikke vil skje i produksjon.
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_MultipleActiveUnexpiredCode_ReturnsNewestCode()
    {
        // Arrange - seeder 2 stk koder med forskjellig tid
        var newestCode = await SeedOtpCodeAsync(createdAt: DateTime.UtcNow);
        await SeedOtpCodeAsync(createdAt: DateTime.UtcNow.AddMinutes(-5));
        
        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(_userId);

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
        await SeedUserAsync(_userId);
        
        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(_userId);

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
        await SeedOtpCodeAsync(expiresAt: DateTime.UtcNow.AddMinutes(-1));
        
        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(_userId);

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
        await SeedOtpCodeAsync(isUsed: true);
        
        // Act
        var existingOtpCode = await _sut.GetActiveCodeAsync(_userId);

        // Assert
        existingOtpCode.Should().BeNull();
    }
    
    /// <summary>
    /// Tester at vi ikke henter en aktiv OtpCode for en annen bruker
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_WrongUserId_ReturnsNull()
    {
        // Arrange - seeder en Otp-kode til en bruker som er aktiv
        await SeedOtpCodeAsync();
        
        // Oppretter en annen bruker uten noen kode
        var otherUserId = Guid.NewGuid();
        await SeedUserAsync(otherUserId, "test123@example.com");
        
        // Act - Kaller metoden med en annen brukerId
        var existingOtpCode = await _sut.GetActiveCodeAsync(otherUserId);

        // Assert
        existingOtpCode.Should().BeNull();
    }
    
    
    public void Dispose() => _context.Dispose();
    
}