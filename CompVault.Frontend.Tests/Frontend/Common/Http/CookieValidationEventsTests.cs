using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Http.Models;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Moq;
namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class CookieValidationEventsTests
{
    private readonly CookieValidationEvents _sut;

    private readonly Mock<IAuthenticationService> _authServiceMock = new();
    private readonly Mock<ITokenRefreshService> _tokenRefreshServiceMock;
    private readonly Mock<IWebHostEnvironment> _envMock = new();

    private readonly string _userId = Guid.NewGuid().ToString();

    public CookieValidationEventsTests()
    {
        Mock<ILogger<CookieValidationEvents>> loggerMock = new();
        _envMock.Setup(x => x.EnvironmentName).Returns(Environments.Development);
        var authSettings = new AuthSettings { CookieExpireDays = 7, ValidationIntervalMinutes = 1 };


        _tokenRefreshServiceMock = new Mock<ITokenRefreshService>();
        _sut = new CookieValidationEvents(loggerMock.Object,
            _tokenRefreshServiceMock.Object,
            authSettings,
            _envMock.Object);
    }

    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------

    /// <summary>
    /// Vi bygger en CookieValidatePrincipalContext som Cookie Middleware sender inn til ValidatePrincipal.
    /// Trenger 4 vitkige elementer: HttpContexen til en forespørsel, en AuthenticationTicket som inneholder
    /// AuthenticationProperties (som inneholder metadata, som feks LastValidated) og en brukers
    /// ClaimsPrincipal (som igjen inneholder claims til innlogget bruker), en AuthenticationScheme som
    /// kreves av konstruktøren og det samme med en CookieAuthenticationOptions
    /// </summary>
    private CookieValidatePrincipalContext CreateValidatePrincipalContext(ClaimsPrincipal? principal = null,
        bool setUserIdClaim = true, bool setRefreshTokenCookie = true)
    {
        // Oppretter en HttpContext med eller uten refresh token cookie
        DefaultHttpContext httpContext = CreateDefaultHttpContext(setUserIdClaim, setRefreshTokenCookie);

        // AuthenticationProperties - Inneholder LastValidated og annen metadata
        var authenticationProperties = new AuthenticationProperties();

        // Hvis ikke vi sender med egen ClaimsPrincipal så bygger vi en selv med utgått access_token
        if (principal == null)
        {
            var accessTokenClaim = new Claim("access_token", "old_token");
            var userIdClaim = new Claim("sub", _userId);
            var claimsIdentity = new ClaimsIdentity([accessTokenClaim, userIdClaim], "Cookie");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            principal = claimsPrincipal;
        }

        var ticket = new AuthenticationTicket(principal, authenticationProperties,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var scheme = new AuthenticationScheme(
            CookieAuthenticationDefaults.AuthenticationScheme,
            displayName: null,
            handlerType: typeof(CookieAuthenticationHandler));

        return new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket);
    }

    // Bygger en HttpContext for forespørselen til backend
    private DefaultHttpContext CreateDefaultHttpContext(bool setUserIdClaim = true, bool setRefreshTokenCookie = true)
    {
        // Registerer klienten og en mocket IAuthenticationService i DI-en
        var services = new ServiceCollection();
        services.AddSingleton(_authServiceMock.Object);

        // ValidatePrincipal kaller SignOutAsync via RequestServices og trenger IAuthenticationService
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        if (setUserIdClaim)
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", _userId)], "Cookie"));
        }

        // Vi setter denne for å kunne sjekke om en cookie er slettet
        httpContext.Features.Set<IHttpResponseFeature>(new HttpResponseFeature
        {
            Headers = new HeaderDictionary()
        });

        if (setRefreshTokenCookie)
            httpContext.Request.Headers.Append("Cookie", "refreshToken=valid_refresh_token");

        return httpContext;
    }

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at brukeren blir logget ut hvis vi får NotFound fra RefreshPairAsync (altså ingen refresh token)
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_NoRefreshToken_UserLoggedOut()
    {
        // Arrange
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(setRefreshTokenCookie: false);

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at det er ingen innloggede brukere og at vi aldri prøver å refreshe token
        context.Principal.Should().BeNull();
        context.HttpContext.Response.Headers.SetCookie
            .Should().ContainMatch("refreshToken=;*");
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once());
    }

    /// <summary>
    /// Tester at brukeren blir logget ut og at token er slettet hvis brukeren har blitt deaktivert/soft deleted
    /// Får ErrorCode Unathorized fra RefreshPairAsync
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_AccountIsDisabled_UserLoggedOut()
    {
        // Arrange - Oppretter et Problem Detail med code AccountInactive
        _tokenRefreshServiceMock
            .Setup(x => x.RefreshPairAsync(_userId, It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unauthorized,
                "Bruker deaktivert")));

        CookieValidatePrincipalContext context = CreateValidatePrincipalContext();

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at brukeren ble utlogget, SignOutAsync ble kalt engang og at cookien er slettet
        context.Principal.Should().BeNull();
        context.HttpContext.Response.Headers.SetCookie
            .Should().ContainMatch("refreshToken=;*");
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once());
    }

    /// <summary>
    /// Tester at andre backend feil som ikke er AccountInactive feiler åpent, når TokenRefreshService
    /// returner ErrorCode Unknown. Brukeren forblir innlogget. Kan skje ved backend nede eller race condition
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_RefreshFailsWithOtherError_KeepsUserLoggedIn()
    {
        // Arrange
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext();

        _tokenRefreshServiceMock
            .Setup(x => x.RefreshPairAsync(_userId, It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown,
                "Token refresh feilet")));

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at brukeren fortsatt er innlogget og at SignoutAsync ikke er kalt
        context.Principal.Should().NotBeNull();
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Never());
    }

    /// <summary>
    /// Tester at hvis vi får ErrorCode RecentlyRefreshed så logges vi ikke ut, men forblir innlogget
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_RecentlyRefreshed_KeepsUserLoggedIn()
    {
        // Arrange
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext();

        _tokenRefreshServiceMock
            .Setup(x => x.RefreshPairAsync(_userId, It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.RecentlyRefreshed,
                "Nylig oppdatert")));

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Brukeren forblir innlogget og SignOutAsync kalles ikke
        context.Principal.Should().NotBeNull();
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Never());
    }

    /// <summary>
    /// Tester happy path ved at vi setter ShouldRenew og at ApplyTokenPair kalles med riktig RefreshRecord
    /// når TokenRefreshService returnerer suksess, oppdateres access token-claim og refresh token-cookie settes
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_ValidRefreshToken_UpdatesAccessTokenClaimAndRefreshCookie()
    {
        // Arrange
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext();

        // Mocker at RefreshPairAsync returnerer RefreshRecord
        var refreshRecord = new RefreshRecord("new_access_token", "new_refresh_token",
            DateTimeOffset.UtcNow);
        _tokenRefreshServiceMock
            .Setup(x => x.RefreshPairAsync(_userId, It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Success(refreshRecord));

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at brukeren er fortsatt innlogget
        context.Principal.Should().NotBeNull();
        context.ShouldRenew.Should().BeTrue();
        context.Principal!.FindFirst("access_token")?.Value.Should().Be("new_access_token");
        context.HttpContext.Response.Headers.SetCookie
            .Should().ContainMatch("refreshToken=new_refresh_token*");
    }

    /// <summary>
    /// Tester at det ikke finnes en sub-claim med UserId. Logger brukern ut og fjernes fra contexten
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_NoSubClaim_UserLoggedOut()
    {
        // Arrange - Principal uten sub-claim
        var claimsIdentity = new ClaimsIdentity([new Claim("access_token", "token")],
            "Cookie");
        var principal = new ClaimsPrincipal(claimsIdentity);
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(principal: principal,
            setUserIdClaim: false);

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at brukere ikke er innlogget og at vi ikke utførte kall mot backend
        context.Principal.Should().BeNull();
        context.HttpContext.Response.Headers.SetCookie
            .Should().ContainMatch("refreshToken=;*");
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once());
        _tokenRefreshServiceMock.Verify(x => x.RefreshPairAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}