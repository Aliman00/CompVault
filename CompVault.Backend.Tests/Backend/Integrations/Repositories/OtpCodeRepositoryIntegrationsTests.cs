using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace CompVault.Backend.Tests.Backend.Integrations.Repositories;

[Collection(nameof(IntegrationTestCollection))]
public class OtpCodeRepositoryIntegrationsTests(
    BackendWebApplicationFactory factory) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private OtpCodeRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await TestDataSeeder.SeedUserAsync(factory.Services, id: TestConstants.Users.ActiveUserId);

        // Oppretter scope for systemet vi tester - gjør det engang i konstruktøren for å slippe 
        // og gjenta dette i hver test
        IServiceScope scope = factory.Services.CreateScope();
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
        OtpCode otpCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services, userId: TestConstants.Users.ActiveUserId);

        // Act
        OtpCode? existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

        // Assert
        existingOtpCode.Should().NotBeNull();
        existingOtpCode.Id.Should().Be(otpCode.Id);
    }

    /// <summary>
    /// Tester at oppretting av to OTP-koder på engang kaster en feil, når begge
    /// </summary>
    [Fact]
    public async Task GetActiveCodeAsync_OneExpiredOneActive_ReturnsActiveCode()
    {
        // Arrange - seeder en utgått og en aktiv kode
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId,
            expiresAt: DateTime.UtcNow.AddMinutes(-20),
            isUsed: true); 
        
        OtpCode activeCode =  await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId); 
        
        // Act
        OtpCode? activeCodeResult = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);
        
        // Assert
        activeCodeResult.Should().NotBeNull();
        activeCodeResult!.Id.Should().Be(activeCode.Id);
    }
    
    /// <summary>
    /// Tester at vårt filter fungerer med å kjøre to parallele requester.
    /// Sikrer at try-catchen sender feilmelding og at det ikke blir to eposter med to forskjellige koder til brukeren
    /// </summary>
    [Fact]
    public async Task GenerateOtpCodeAsync_ConcurrentRequests_OnlyOneSucceeds()
    {
        // Arrange
        await using AsyncServiceScope scope1 = factory.Services.CreateAsyncScope();
        await using AsyncServiceScope scope2 = factory.Services.CreateAsyncScope();

        IOtpCodeService sut1 = scope1.ServiceProvider.GetRequiredService<IOtpCodeService>();
        IOtpCodeService sut2 = scope2.ServiceProvider.GetRequiredService<IOtpCodeService>();

        // Barrier brukes til å sikre at at begge kall kjøres parallelt
        var barrier = new Barrier(2);

        // Act - Setter opp to oppgaver, et for hvert kall, og kaller de samtidig
        var task1 = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await sut1.GenerateOtpCodeAsync(TestConstants.Users.ActiveUserId);
        });

        var task2 = Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await sut2.GenerateOtpCodeAsync(TestConstants.Users.ActiveUserId);
        });
        
        Result<string>[] results = await Task.WhenAll(task1, task2);

        // Assert - Teller at et kall var vellykket og at et kall skal få OtpCooldown fra try-catch
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.IsFailure && r.Error!.Code == ErrorCode.OtpCooldown).Should().Be(1);
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
        OtpCode? existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

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
            userId: TestConstants.Users.ActiveUserId, expiresAt: DateTime.UtcNow.AddMinutes(-1));

        // Act
        OtpCode? existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

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
            userId: TestConstants.Users.ActiveUserId, isUsed: true);

        // Act
        OtpCode? existingOtpCode = await _sut.GetActiveCodeAsync(TestConstants.Users.ActiveUserId);

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

        ApplicationUser userWithoutCode = await TestDataSeeder.SeedUserAsync(factory.Services, email: "userb@compvault.com");

        // Act - Kaller metoden med en annen brukerId
        OtpCode? existingOtpCode = await _sut.GetActiveCodeAsync(userWithoutCode.Id);

        // Assert
        existingOtpCode.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // DeleteExpiredCodesAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at vi sletter alle utgåtte OTP-koder
    /// </summary>
    [Fact]
    public async Task DeleteExpiredCodesAsync_ExpiredCode_DeletesCode()
    {
        // Arrange - oppretter utgått token
        OtpCode expiredCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            expiresAt: DateTime.UtcNow.AddMinutes(-30));

        // Act
        await _sut.DeleteExpiredCodesAsync();

        // Assert
        bool codeExists = await _context.Set<OtpCode>()
            .AnyAsync(r => r.Id == expiredCode.Id);
        codeExists.Should().BeFalse();
    }

    /// <summary>
    /// Tester at vi sletter alle brukte OTP-koder
    /// </summary>
    [Fact]
    public async Task DeleteExpiredCodesAsync_UsedCode_DeletesCode()
    {
        // Arrange
        OtpCode usedCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services, isUsed: true);

        // Act
        await _sut.DeleteExpiredCodesAsync();

        // Assert
        bool codeExists = await _context.Set<OtpCode>()
            .AnyAsync(r => r.Id == usedCode.Id);
        codeExists.Should().BeFalse();
    }

    /// <summary>
    /// Tester at vi ikke sletter aktive OTP-koder
    /// </summary>
    [Fact]
    public async Task DeleteExpiredCodesAsync_ActiveCode_DoesNotDeleteCode()
    {
        // Arrange
        OtpCode activeCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services);

        // Act
        await _sut.DeleteExpiredCodesAsync();

        // Assert
        bool codeExists = await _context.Set<OtpCode>()
            .AnyAsync(r => r.Id == activeCode.Id);
        codeExists.Should().BeTrue();
    }
    
    // -------------------------------------------------------------------------
    // DeleteExpiredCodesAsync
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at vi sletter alle utgåtte OTP-koder for en bruker
    /// </summary>
    [Fact]
    public async Task DeleteExpiredForUserAsync_ExpiredCode_DeletesCode()
    {
        // Arrange - oppretter utgått token
        OtpCode expiredCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId,
            expiresAt: DateTime.UtcNow.AddMinutes(-30));

        // Act
        await _sut.DeleteExpiredForUserAsync(TestConstants.Users.ActiveUserId, CancellationToken.None);
        await _context.SaveChangesAsync(); 

        // Assert
        bool codeExists = await _context.Set<OtpCode>()
            .AnyAsync(r => r.Id == expiredCode.Id);
        codeExists.Should().BeFalse();
    }

    /// <summary>
    /// Tester at vi sletter alle brukte OTP-koder for en bruker
    /// </summary>
    [Fact]
    public async Task DeleteExpiredForUserAsync_UsedCode_DeletesCode()
    {
        // Arrange
        OtpCode usedCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services, 
            userId: TestConstants.Users.ActiveUserId,
            isUsed: true);

        // Act
        await _sut.DeleteExpiredForUserAsync(TestConstants.Users.ActiveUserId, CancellationToken.None);
        await _context.SaveChangesAsync(); 

        // Assert
        bool codeExists = await _context.Set<OtpCode>()
            .AnyAsync(r => r.Id == usedCode.Id);
        codeExists.Should().BeFalse();
    }

    /// <summary>
    /// Tester at vi ikke sletter aktive OTP-koder for en bruker
    /// </summary>
    [Fact]
    public async Task DeleteExpiredForUserAsync_ActiveCode_DoesNotDeleteCode()
    {
        // Arrange
        OtpCode activeCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: TestConstants.Users.ActiveUserId);

        // Act
        await _sut.DeleteExpiredForUserAsync(TestConstants.Users.ActiveUserId, CancellationToken.None);
        await _context.SaveChangesAsync(); 

        // Assert
        bool codeExists = await _context.Set<OtpCode>()
            .AnyAsync(r => r.Id == activeCode.Id);
        codeExists.Should().BeTrue();
    }
    
    /// <summary>
    /// Tester at vi ikke sletter en annen brukers OTP-kode
    /// </summary>
    [Fact]
    public async Task DeleteExpiredForUserAsync_ExpiredCodeForOtherUser_DoesNotDeleteCode()
    {
        ApplicationUser otherUser = await TestDataSeeder.SeedUserAsync(factory.Services, email: "test2@compvault.com");
        OtpCode expiredCode = await TestDataSeeder.SeedOtpCodeAsync(factory.Services,
            userId: otherUser.Id,
            expiresAt: DateTime.UtcNow.AddMinutes(-30));

        await _sut.DeleteExpiredForUserAsync(TestConstants.Users.ActiveUserId, CancellationToken.None);
        await _context.SaveChangesAsync(); 

        bool codeExists = await _context.Set<OtpCode>()
            .AnyAsync(r => r.Id == expiredCode.Id);
        codeExists.Should().BeTrue();
    }

}