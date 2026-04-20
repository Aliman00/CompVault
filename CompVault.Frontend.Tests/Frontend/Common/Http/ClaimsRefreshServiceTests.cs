using System.Security.Claims;

using CompVault.Frontend.Common.Http.Models;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

using Moq;

namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class ClaimsRefreshServiceTests
{
    private readonly Mock<ITokenRefreshService> _tokenRefreshServiceMock = new();
    private readonly Mock<AuthStateProvider> _authStateProviderMock;
    private readonly CircuitUserContext _circuitUserContext = new();
    private readonly ClaimsRefreshService _sut;

    public ClaimsRefreshServiceTests()
    {
        Mock<ILogger<ClaimsRefreshService>> loggerMock = new();
        _authStateProviderMock = new Mock<AuthStateProvider>(_circuitUserContext);

        _sut = new ClaimsRefreshService(
            _tokenRefreshServiceMock.Object,
            _circuitUserContext,
            _authStateProviderMock.Object,
            loggerMock.Object);
    }

    // Tilfeldig Guid for å teste mot ekte Guid
    private const string UserId = "019d8dcb-2342-7cc5-add6-ae0420f990c3";
    private const string RefreshToken = "refresh-token";

    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------

    private void SetUserInCircuitUserContext(bool withRefreshToken = true)
    {
        var subClaim = new Claim("sub", UserId);
        var accessTokenClaim = new Claim("access_token", "access_token");
        var identity = new ClaimsIdentity([subClaim, accessTokenClaim]);

        if (withRefreshToken)
            _circuitUserContext.SetUser(new ClaimsPrincipal(identity), RefreshToken);
        else
            _circuitUserContext.SetUser(new ClaimsPrincipal(identity), string.Empty);

    }

    // -------------------------------------------------------------------------
    // Happy paths
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at tokens blir refreshet og oppdatert manuelt ved kall til RefreshTokenAsync i komponenter.
    /// Sikrer at CiruitUserContext har riktig brukerinfo i kretsen
    /// </summary>
    [Fact]
    public async Task RefreshTokensAsync_ValidUser_UpdatesTokensAndUI()
    {
        // Arrange - Legger inn en bruker og oppretter et refreshRecord som kommer fra backend
        SetUserInCircuitUserContext();
        var refreshRecord = new RefreshRecord("new_access_token", "new_refresh_token",
            DateTimeOffset.UtcNow);

        // Mocker RefreshPairAsync til å returerne RefreshRecord
        _tokenRefreshServiceMock
            .Setup(x => x.RefreshPairAsync(UserId, RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Success(refreshRecord));

        // Mocker at vi henter brukeren vi opprettet
        _authStateProviderMock
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(_circuitUserContext.User));

        // Act
        await _sut.RefreshTokensAsync();

        // Assert - Sjekker at token og user er oppdatert og at vi har invalidert cooldown to ganger
        _circuitUserContext.RefreshToken.Should().Be("new_refresh_token");
        _circuitUserContext.User.FindFirst("access_token")?.Value.Should().Be("new_access_token");
        _authStateProviderMock.Verify(x => x.GetAuthenticationStateAsync(), Times.Once());
        _tokenRefreshServiceMock.Verify(x => x.InvalidateCooldown(UserId), Times.Exactly(2));
    }

    // -------------------------------------------------------------------------
    // Failure paths
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at CircuitUserContext ikke har en bruker og sikrer at vi returnerer tidlig før vi invaldiere cooldown
    /// </summary>
    [Fact]
    public async Task RefreshTokensAsync_NoUserId_ReturnsEarly()
    {
        // Arrange - Ingen opprettete brukere

        // Act
        await _sut.RefreshTokensAsync();

        // Assert - Sjekker at vi returnerer tidlig og ikke invaliderer cooldown
        _tokenRefreshServiceMock.Verify(x => x.InvalidateCooldown(UserId), Times.Never());
    }

    /// <summary>
    /// Tester at CircuitUserContext ikke har refresh token  og sikrer at vi returnerer tidlig
    /// </summary>
    [Fact]
    public async Task RefreshTokensAsync_NoRefreshToken_ReturnsEarly()
    {
        // Arrange - Bruker uten RefreshToken
        SetUserInCircuitUserContext(false);

        // Act
        await _sut.RefreshTokensAsync();

        // Assert - Sjekker at vi returnerer tidlig og ikke invaliderer cooldown
        _tokenRefreshServiceMock.Verify(x => x.InvalidateCooldown(UserId), Times.Never());
    }

    /// <summary>
    /// Tester at backend gir feil på RefreshPairAsync og vi oppdaterer ikke CircuitUserContext eller
    /// AuthenticationState (UI)
    /// </summary>
    [Fact]
    public async Task RefreshTokensAsync_RefreshFails_DoesNotUpdateClaimsOrUI()
    {
        // Arrange - Legger inn en bruker og oppretter et refreshRecord som kommer fra backend
        SetUserInCircuitUserContext();

        // Mocker RefreshPairAsync til å returnere en feil fra backend
        _tokenRefreshServiceMock
            .Setup(x => x.RefreshPairAsync(UserId, RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Failure(
                AppError.Create(ErrorCode.TokenExpired, "Token expired")));

        // Act
        await _sut.RefreshTokensAsync();

        // Assert - Sjekker at token er original token og at GetAuthenticationStateAsync ikke er kalt
        _circuitUserContext.User.FindFirst("access_token")?.Value.Should().Be("access_token");
        _authStateProviderMock.Verify(x => x.GetAuthenticationStateAsync(), Times.Never());
    }

}