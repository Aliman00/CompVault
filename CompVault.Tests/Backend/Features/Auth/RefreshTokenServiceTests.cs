using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using FluentAssertions;
using Moq;

namespace CompVault.Tests.Backend.Features.Auth;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repositoryMock;
    private readonly RefreshTokenService _sut;

    public RefreshTokenServiceTests()
    {
        _repositoryMock = new Mock<IRefreshTokenRepository>();
        Mock<IJwtService> jwtServiceMock = new Mock<IJwtService>();

        // Mocker RefreshTokenLifetimeDays fra IJwtService
        jwtServiceMock
            .Setup(x => x.RefreshTokenLifetimeDays)
            .Returns(7);
        
        _sut = new RefreshTokenService(
            jwtServiceMock.Object,
            _repositoryMock.Object);
    }
    
    /// <summary>
    /// Tester at CreateRefreshTokenAsync lagrer token med riktige egenskaper
    /// </summary>
    [Fact]
    public async Task CreateRefreshTokenAsync_SavesTokenWithCorrectProperties()
    {
        // Arrange - Oppretter en bruker-ID og variabel for å fange opp RefreshToken
        var userId = Guid.NewGuid();
        RefreshToken? capturedToken = null;
        
        // Henter RefreshToken som metoden bygger
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => capturedToken = token);

        // Act
        var result = await _sut.CreateRefreshTokenAsync(userId);

        // Assert - Sjekker at token har riktig egenskaper 
        result.IsSuccess.Should().BeTrue();
        capturedToken.Should().NotBeNull();
        capturedToken!.UserId.Should().Be(userId);
        capturedToken.IsRevoked.Should().BeFalse();
        capturedToken.ExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }
    
    /// <summary>
    /// Tester at GenerateRefreshToken returnerer en unik, ikke-tom Base64-streng
    /// </summary>
    [Fact]
    public void GenerateRefreshToken_ReturnsTwoUniqueTokens()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert - Skal ikke være tom og to kall skal aldri gi samme token
        Assert.False(string.IsNullOrEmpty(token1));
        Assert.NotEqual(token1, token2);
    }
    
   
}