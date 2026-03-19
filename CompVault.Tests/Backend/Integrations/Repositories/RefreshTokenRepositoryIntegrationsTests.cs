using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Tests.Common;
using CompVault.Tests.Common.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Tests.Backend.Integrations.Repositories;

public class RefreshTokenRepositoryIntegrationsTests(
    BackendWebApplicationFactory factory) : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
{
    
    private AppDbContext _context = null!;
    private RefreshTokenRepository _sut = null!;
    
    public async Task InitializeAsync()
    {
        await TestDataSeeder.CreateDb(factory.Services);
        await TestDataSeeder.SeedUserAsync(factory.Services, id: TestConstants.Users.ActiveUserId);
        
        // Oppretter scope for systemet vi tester - gjør det engang i konstruktøren for å slippe 
        // og gjenta dette i hver test
        var scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _sut = new RefreshTokenRepository(_context);
    }

    public Task DisposeAsync() => Task.CompletedTask;
    
    
    
    // -------------------------------------------------------------------------
    // GetValidTokenAsync
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at en valid refresh token eksisterer
    /// </summary>
    [Fact]
    public async Task GetValidTokenAsync_TokenExists_ReturnsToken()
    {
        // Arrange
        var refreshToken = await TestDataSeeder.SeedRefreshTokenAsync(factory.Services);
        
        // Act
        var validRefreshToken = await _sut.GetValidTokenAsync(refreshToken.Token);
        
        // Assert
        validRefreshToken.Should().NotBeNull();
        validRefreshToken.Id.Should().Be(refreshToken.Id);
    }
    
    /// <summary>
    /// Tester at innsendt token gir null
    /// </summary>
    [Fact]
    public async Task GetValidTokenAsync_TokenDoesNotExist_ReturnsNull()
    {
        // Arrange
        var token = "testtoken";
        
        // Act
        var storedRefreshToken = await _sut.GetValidTokenAsync(token);
        
        // Assert
        storedRefreshToken.Should().BeNull();
    }
    
    /// <summary>
    /// Tester at en revoked token returnerer null
    /// </summary>
    [Fact]
    public async Task GetValidTokenAsync_TokenIsRevoked_ReturnsNull()
    {
        // Arrange
        var refreshToken = await TestDataSeeder.SeedRefreshTokenAsync(factory.Services, isRevoked: true);
        
        // Act
        var storedRefreshToken = await _sut.GetValidTokenAsync(refreshToken.Token);
        
        // Assert
        storedRefreshToken.Should().BeNull();
    }
    
    /// <summary>
    ///  Tester at utgått token returnerer null
    /// </summary>
    [Fact]
    public async Task GetValidTokenAsync_TokenIsExpired_ReturnsNull()
    {
        // Arrange
        var refreshToken = await TestDataSeeder.SeedRefreshTokenAsync(factory.Services, 
            expiresAt: DateTime.UtcNow.AddMinutes(-30));
        
        // Act
        var storedRefreshToken = await _sut.GetValidTokenAsync(refreshToken.Token);
        
        // Assert
        storedRefreshToken.Should().BeNull();
    }
    
    /// <summary>
    /// Tester at vi riktig token blir hentet når det eksisterer flere tokens i databasen
    /// </summary>
    [Fact]
    public async Task GetValidTokenAsync_MultipleValidTokenExists_ReturnsCorrectToken()
    {
        // Arrange - Seeder bruker og token for to forskjellige brukere
        var refreshTokenA = await TestDataSeeder.SeedRefreshTokenAsync(factory.Services);
        
        var userB = await TestDataSeeder.SeedUserAsync(factory.Services, email: "userb@compvault.com");
        await TestDataSeeder.SeedRefreshTokenAsync(factory.Services, 
            userId: userB.Id);
        
        // Act
        var validRefreshToken = await _sut.GetValidTokenAsync(refreshTokenA.Token);
        
        // Assert
        validRefreshToken.Should().NotBeNull();
        validRefreshToken.Id.Should().Be(refreshTokenA.Id);
    }
    
    
    // -------------------------------------------------------------------------
    // DeleteExpiredTokenAsync
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at vi sletter alle utgåtte tokens
    /// </summary>
    [Fact]
    public async Task DeleteExpiredTokenAsync_ExpiredToken_DeletesToken()
    {
        // Arrange
        var expiredToken = await TestDataSeeder.SeedRefreshTokenAsync(factory.Services,
            expiresAt: DateTime.UtcNow.AddMinutes(-30));
        
        // Act
        await _sut.DeleteExpiredTokensAsync();
        
        // Assert
        var tokenExists = await _context.Set<RefreshToken>()
            .AnyAsync(r => r.Id == expiredToken.Id);
        tokenExists.Should().BeFalse();
    }
    
    /// <summary>
    /// Tester at vi sletter alle revoked tokens
    /// </summary>
    [Fact]
    public async Task DeleteExpiredTokenAsync_RevokedToken_DeletesToken()
    {
        // Arrange
        var revokedToken = await TestDataSeeder.SeedRefreshTokenAsync(factory.Services, isRevoked: true);
        
        // Act
        await _sut.DeleteExpiredTokensAsync();
        
        // Assert
        var tokenExists = await _context.Set<RefreshToken>()
            .AnyAsync(r => r.Id == revokedToken.Id);
        tokenExists.Should().BeFalse();
    }
    
    /// <summary>
    /// Tester at vi ikke sletter aktive tokens
    /// </summary>
    [Fact]
    public async Task DeleteExpiredTokenAsync_ActiveToken_DoesNotDeleteToken()
    {
        // Arrange
        var refreshToken = await TestDataSeeder.SeedRefreshTokenAsync(factory.Services);
        
        // Act
        await _sut.DeleteExpiredTokensAsync();
        
        // Assert
        var tokenExists = await _context.Set<RefreshToken>()
            .AnyAsync(r => r.Id == refreshToken.Id);
        tokenExists.Should().BeTrue();
    }
}
