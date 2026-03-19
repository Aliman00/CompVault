using CompVault.Backend.Infrastructure.Repositories.Auth;
using FluentAssertions;



namespace CompVault.Tests.Backend.Infrastructure.Repositories;

public class RefreshTokenRepositoryTests : InMemoryRepositoryBase
{
    private readonly RefreshTokenRepository _sut;

    public RefreshTokenRepositoryTests()
    {
        _sut = new RefreshTokenRepository(Context);
    }
    
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
        var user = await SeedUserAsync();
        var refreshToken = await SeedRefreshTokenAsync(userId: user.Id);
        
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
        var user = await SeedUserAsync();
        var refreshToken = await SeedRefreshTokenAsync(userId: user.Id, isRevoked: true);
        
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
        var user = await SeedUserAsync();
        var refreshToken = await SeedRefreshTokenAsync(userId: user.Id, expiresAt: DateTime.UtcNow.AddMinutes(-30));
        
        // Act
        var storedRefreshToken = await _sut.GetValidTokenAsync(refreshToken.Token);
        
        // Assert
        storedRefreshToken.Should().BeNull();
    }
    
    /// <summary>
    /// Tester at vi riktig token blir hentet når det eksisterer flere tokens i databasen
    /// </summary>
    [Fact]
    public async Task GetValidTokenAsync_MutipleValidTokenExists_ReturnsCorrectToken()
    {
        // Arrange - Seeder bruker og token for to forskjellige brukere
        var userA = await SeedUserAsync();
        var refreshTokenUserA = await SeedRefreshTokenAsync(userId: userA.Id);
        
        var userB = await SeedUserAsync(email: "userb@compvault.com");
        await SeedRefreshTokenAsync(userId: userB.Id);
        
        // Act
        var validRefreshToken = await _sut.GetValidTokenAsync(refreshTokenUserA.Token);
        
        // Assert
        validRefreshToken.Should().NotBeNull();
        validRefreshToken.Id.Should().Be(refreshTokenUserA.Id);
    }
}