using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;

namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class CookieValidationEventsTests
{
    private readonly CookieValidationEvents _sut;

    private readonly Mock<HttpMessageHandler> _authClientHandlerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IAuthenticationService> _authServiceMock = new();

    private const string SendAsync = "SendAsync";
    private const string BaseAddress = "https://backend";

    public CookieValidationEventsTests()
    {
        // Standard mocks for konstruktøren
        var authSettings = new AuthSettings { ValidationIntervalMinutes = 10 };
        var envMock = new Mock<IWebHostEnvironment>();
        Mock<ILogger<CookieValidationEvents>> loggerMock = new();

        // Mocker HttpClientFactory til å bruke Auth-klienten
        _authClientHandlerMock = new Mock<HttpMessageHandler>();
        var authHttpClient = new HttpClient(_authClientHandlerMock.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };

        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(BackendApiSettings.AuthClientName))
            .Returns(authHttpClient);

        _sut = new CookieValidationEvents(authSettings, envMock.Object, loggerMock.Object);
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
    private CookieValidatePrincipalContext CreateValidatePrincipalContext(string? lastValidated = null,
        string? refreshTokenCookie = null, ClaimsPrincipal? principal = null)
    {
        // Oppretter en HttpContext med eller uten refresh token cookie
        DefaultHttpContext httpContext = CreateDefaultHttpContext(refreshTokenCookie);

        // AuthenticationProperties - Inneholder LastValidated og annen metadata
        var authenticationProperties = new AuthenticationProperties();
        if (lastValidated != null)
            authenticationProperties.SetParameter("LastValidated", lastValidated);

        // Hvis ikke vi sender med egen ClaimsPrincipal så bygger vi en selv med utgått access_token
        if (principal == null)
        {
            var accessTokenClaim = new Claim("access_token", "old_token");
            var claimsIdentity = new ClaimsIdentity([accessTokenClaim], "Cookie");
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

    // Bygger en HttpContext for forespørselen til backend. Med eller uten refresh token cookie i headeren
    private DefaultHttpContext CreateDefaultHttpContext(string? refreshTokenCookie = null)
    {
        // Registerer klienten og en mocket IAuthenticationService i DI-en
        var services = new ServiceCollection();
        services.AddSingleton(_httpClientFactoryMock.Object);
        services.AddSingleton(_authServiceMock.Object);

        // ValidatePrincipal kaller RequestServices og trenger da å finne både en IHttpClientFactory og en
        // IAuthenticationService
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        // Vi setter denne for å kunne sjekke om en cookie er slettet
        httpContext.Features.Set<IHttpResponseFeature>(new HttpResponseFeature
        {
            Headers = new HeaderDictionary()
        });

        if (refreshTokenCookie != null)
            httpContext.Request.Headers.Append("Cookie", $"refreshToken={refreshTokenCookie}");

        return httpContext;
    }

    // -------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at vi returner tidlig ved at vi sjekket LastValidated for 5 minutter siden
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_LastValidated5MinutesAgo_NoRefreshNeeded()
    {
        // Arrange - Setter opp at vi gjorde en sjekk for 5 min siden
        string lastValidated = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O");
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(lastValidated);

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - sjekker at principal er satt og at vi aldri prøver å refreshe token
        context.Principal.Should().NotBeNull();
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Never(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    /// <summary>
    /// Tester at brukeren blir logget ut hvis vi ikke har noen refresh token
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_NoRefreshToken_UserLoggedOut()
    {
        // Arrange
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext();

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at det er ingen innloggede brukere og at vi aldri prøver å refreshe token
        context.Principal.Should().BeNull();
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Never(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once());
    }

    /// <summary>
    /// Tester at brukeren blir logget ut og at token er slettet hvis brukeren har blitt deaktivert/soft deleted
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_AccountIsDisabled_UserLoggedOut()
    {
        // Arrange - Oppretter et Problem Detail med code AccountInactive
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(refreshTokenCookie: "valid_token");

        var problemDetail = new ProblemDetail
        {
            Status = 403,
            Code = nameof(ErrorCode.AccountInactive),
            Message = "Bruker er deaktivert"
        };

        // Mocker at Refresh token returnerer forbidden med problemdetail
        _authClientHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = JsonContent.Create(problemDetail)
            });

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at brukeren ble utlogget, SignOutAsync ble kalt engang og at cookien er slettet
        context.Principal.Should().BeNull();
        context.HttpContext.Response.Headers.SetCookie.Should().ContainMatch("refreshToken=*");
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Once());
    }

    /// <summary>
    /// Tester at andre backend feil som ikke er AccountInactive feiler åpent.
    /// Brukeren forblir innlogget. Kan forekomme ved f.eks. race condition når to requester prøver å refreshe token
    /// samtidig
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_RefreshFailsWithOtherError_KeepsUserLoggedIn()
    {
        // Arrange
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(refreshTokenCookie: "valid_token");

        _authClientHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Conflict));

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at brukeren fortsatt er innlogget og at SignoutAsync ikke er kalt
        context.Principal.Should().NotBeNull();
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _authServiceMock.Verify(x => x.SignOutAsync(
            It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties?>()), Times.Never());
    }

    /// <summary>
    /// Tester happy path ved at brukeren får nytt token par, claim er oppdatert og nye cookies
    /// </summary>
    [Fact]
    public async Task ValidatePrincipal_ValidRefreshToken_UpdatesAccessTokenClaimAndRefreshCookie()
    {
        // Arrange
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(refreshTokenCookie: "valid_token");

        // Mocker at Refresh token returnerer OK med nye tokens
        _authClientHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse
                {
                    AccessToken = "new_access_token",
                    RefreshToken = "new_refresh_token"
                })
            });

        // Act
        await _sut.ValidatePrincipal(context);

        // Assert - Sjekker at brukeren er fortsatt innlogget, ny claim og refresh token-cookie er satt
        context.Principal.Should().NotBeNull();

        string? newClaim = context.Principal?.FindFirst("access_token")?.Value;
        newClaim.Should().Be("new_access_token");
        context.HttpContext.Response.Headers.SetCookie
            .Should().ContainMatch("refreshToken=new_refresh_token*");

        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
}