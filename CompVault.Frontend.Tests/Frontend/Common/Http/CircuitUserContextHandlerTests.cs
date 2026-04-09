using System.Security.Claims;

using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Services;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Moq;
namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class CircuitUserContextHandlerTests
{
    private readonly CircuitUserContext _circuitUserContext = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
    private readonly CircuitUserContextHandler _sut;

    public CircuitUserContextHandlerTests()
    {
        _sut = new CircuitUserContextHandler(_circuitUserContext, _httpContextAccessorMock.Object);
    }

    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------

    // Bygger en HttpContext med eller uten en autorisert bruker, og med eller uten Refresh Token-cookie
    private DefaultHttpContext BuildHttpContext(bool isAuthenticated = true,
        string? refreshToken = "valid_refresh_token")
    {
        var httpContext = new DefaultHttpContext();

        if (isAuthenticated)
        {
            var identity = new ClaimsIdentity(
                [new Claim("sub", "user-123")], authenticationType: "Cookie");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        if (refreshToken != null)
            httpContext.Request.Headers.Append("Cookie", $"refreshToken={refreshToken}");

        return httpContext;
    }

    // -------------------------------------------------------------------------
    // OnConnectionUpAsync tester
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester happy path — autentisert bruker med refresh token cookie setter CircuitUserContext korrekt
    /// </summary>
    [Fact]
    public async Task OnConnectionUpAsync_AuthenticatedUserWithRefreshToken_SetsCircuitUserContext()
    {
        // Arrange
        DefaultHttpContext httpContext = BuildHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _sut.OnConnectionUpAsync(null!, CancellationToken.None);

        // Assert
        _circuitUserContext.User.Should().BeSameAs(httpContext.User);
        _circuitUserContext.RefreshToken.Should().Be("valid_refresh_token");
    }

    /// <summary>
    /// Tester at CircuitUserContext ikke oppdateres hvis brukeren ikke er autentisert
    /// </summary>
    [Fact]
    public async Task OnConnectionUpAsync_UnauthenticatedUser_DoesNotSetCircuitUserContext()
    {
        // Arrange
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(BuildHttpContext(isAuthenticated: false));

        // Act
        await _sut.OnConnectionUpAsync(null!, CancellationToken.None);

        // Assert — Tom ClaimsPrincipal
        _circuitUserContext.User.Identity?.IsAuthenticated.Should().BeFalse();
        _circuitUserContext.RefreshToken.Should().BeNull();
    }

    /// <summary>
    /// Tester at CircuitUserContext ikke oppdateres hvis ingen refresh token finnes i cookie
    /// eller som fallback i CircuitUserContext
    /// </summary>
    [Fact]
    public async Task OnConnectionUpAsync_AuthenticatedUserWithNoRefreshToken_DoesNotSetCircuitUserContext()
    {
        // Arrange
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(BuildHttpContext(refreshToken: null));

        // Act
        await _sut.OnConnectionUpAsync(null!, CancellationToken.None);

        // Assert
        _circuitUserContext.User.Identity?.IsAuthenticated.Should().BeFalse();
        _circuitUserContext.RefreshToken.Should().BeNull();
    }

    /// <summary>
    /// Tester at eksisterende refresh token i CircuitUserContext brukes som fallback
    /// hvis ingen cookie finnes i HttpContext — f.eks. ved reconnect etter token-refresh
    /// </summary>
    [Fact]
    public async Task OnConnectionUpAsync_NoRefreshTokenCookieButCircuitContextHasToken_SetsCircuitUserContext()
    {
        // Arrange — simulerer reconnect der refresh token ble oppdatert inne i aktiv krets
        _circuitUserContext.SetUser(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "existing_refresh_token");

        DefaultHttpContext httpContext = BuildHttpContext(refreshToken: null);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _sut.OnConnectionUpAsync(null!, CancellationToken.None);

        // Assert — bruker og token fra fallback
        _circuitUserContext.User.Should().BeSameAs(httpContext.User);
        _circuitUserContext.RefreshToken.Should().Be("existing_refresh_token");
    }
}