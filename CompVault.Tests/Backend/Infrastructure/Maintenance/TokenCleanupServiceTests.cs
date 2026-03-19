using CompVault.Backend.Infrastructure.Maintenance;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompVault.Tests.Backend.Infrastructure.Maintenance;

public class TokenCleanupServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IOtpCodeRepository> _otpCodeRepositoryMock = new();
    private readonly Mock<ILogger<TokenCleanupService>> _loggerMock = new();
    private readonly TokenCleanupService  _sut;

    public TokenCleanupServiceTests()
    {
        _sut = new TokenCleanupService(
            _refreshTokenRepositoryMock.Object,
            _otpCodeRepositoryMock.Object,
            _loggerMock.Object);
    }
    
    /// <summary>
    /// Tester at begge repositoriene blir kalt engang
    /// </summary>
    [Fact]
    public async Task RunCleanupAsync_Success_CallsBothRepositories()
    {
        // Act
        await _sut.RunCleanupAsync(CancellationToken.None);
        
        // Assert
        _refreshTokenRepositoryMock.Verify(
            x => x.DeleteExpiredTokensAsync(It.IsAny<CancellationToken>()), Times.Once);
        _otpCodeRepositoryMock.Verify(
            x => x.DeleteExpiredCodesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
    }
    
    
    /// <summary>
    /// Tester at vi håndterer feil som oppstår slik at jobben ikke stopper opp og at applikasjonen ikke kræsjer
    /// </summary>
    [Fact]
    public async Task RunCleanupAsync_RepositoryThrows_LogsAndHandlesError()
    {
        // Arrange
        _refreshTokenRepositoryMock
            .Setup(x => x.DeleteExpiredTokensAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database down"));
        
        // Act
        var act = async () => await _sut.RunCleanupAsync(CancellationToken.None);
        
        // Assert - Sjekker at RunCleanupAsync fanger exception og logger den
        await act.Should().NotThrowAsync();
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

    }
    
    /// <summary>
    /// Tester at vi ikke fanger opp OperationCancelException og at jobben stopper
    /// </summary>
    [Fact]
    public async Task RunCleanupAsync_CancellationRequested_CancelsOperation()
    {
        // Arrange
        _refreshTokenRepositoryMock
            .Setup(x => x.DeleteExpiredTokensAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        
        // Act
        var act = async () => await _sut.RunCleanupAsync(CancellationToken.None);
        
        // Assert - Sjekker at exception ikke blir fanget opp. s
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}