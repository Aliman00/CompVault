using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Http.Models;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.Result;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class AccessTokenHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    // Vi mocker selve handleren av HttpClienten - for main og auth
    private readonly Mock<HttpMessageHandler> _mainHandlerMock;
    private readonly Mock<ITokenRefreshService> _tokenRefreshService;
    private readonly CircuitUserContext _circuitUserContext = new();
    
    private readonly AccessTokenHandler _sut;
    
    private const string SendAsync = "SendAsync";
    private const string BaseAddress = "https://backend";
    private const string TestEndpoint = $"{BaseAddress}/api/test";
    
    public AccessTokenHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        Mock<ILogger<AccessTokenHandler>> loggerMock = new();
        _tokenRefreshService = new Mock<ITokenRefreshService>();
        
        // mocker base.SendAsync - hva backend svarer på vanlige API-kall
        _mainHandlerMock = new Mock<HttpMessageHandler>();

        _sut = new AccessTokenHandler(
            _httpContextAccessorMock.Object,
            loggerMock.Object,
            _tokenRefreshService.Object,
            _circuitUserContext);

        _sut.InnerHandler = _mainHandlerMock.Object;
    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    // Oppretter innlogget bruker, ved å bygge en claim med gyldig token, lager en ClaimsIdentity med den
    // og bruker den til å sette en bruker med gydlig token i HttpContext. Legger til en refresh token-cookie
    private DefaultHttpContext BuildHttpContext()
    {
        var accessTokenClaim = new Claim("access_token", "valid_access_token");
        var claimsIdentity = new ClaimsIdentity([accessTokenClaim], "Cookie");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        
        httpContext.Request.Headers.Append("Cookie", "refreshToken=valid_refresh_token");
        
        return httpContext;
    }
    
    
    // -------------------------------------------------------------------------
    // SendAsync Happy Paths
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at første response fra backend er 200 Ok. Vi tester da at vi ikke prøver å refreshe token
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIsNot401_ReturnsResponse()
    {
        // Arrange
        DefaultHttpContext httpContext = BuildHttpContext();
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker en HttpResponse med Ok 200 fra backend. Den er protected, så da kan vi bruke Moc sin Protected
        // for å sette opp at responsen til Http-forespørselen er 200 OK
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        
        // Act - Vi sender et HttpRequestMessage-objekt som returnerer responsen fra _mainHandlerMock.
        // Kjører koden i SendAsync
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);
        
        // Assert - Sjekker at responsen er 200 og at refresh token ikke ble oppdatert
        // eller klonende requesten ble sendt
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _tokenRefreshService.Verify(x => x.RefreshPairAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
    
    /// <summary>
    /// Tester at vi får 401 på første, men at vi vellykket refresher token og at den klonede responsen gir 200 Ok
    /// Testen sjekker at klientene blir kalt korrekt antall ganger
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndValidRefreshToken_RefreshesTokenAndReturnsResponse()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext();
        
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "user-id"),
                new Claim("access_token", "valid_access_token")], "Cookie"));
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unauthorized og deretter 200 Ok. Vi bruker en teller for å kunne sikre
        // at vi får forskjellige responser pga samme SendAsync blir kalt to ganger
        int callNumber = 0;
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callNumber++;
                return callNumber == 1 
                    ? new HttpResponseMessage(HttpStatusCode.Unauthorized) 
                    : new HttpResponseMessage(HttpStatusCode.OK);
            });
        
        // Mocker at TokenRefreshService returnerer gyldig RefreshRecord
        var refreshRecord = new RefreshRecord("new_access_token", "new_refresh_token", 
            DateTimeOffset.UtcNow);
        _tokenRefreshService
            .Setup(x => x.RefreshPairAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Success(refreshRecord));
        
        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);
        
        // Assert - Sjekker korrekt statuskode og at klientene ble kalt riktig antall gangner. Main klient 2 ganger,
        // og at tokenRefreshService blir kalt for å fornye token og sette token
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mainHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _tokenRefreshService.Verify(x => x.RefreshPairAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once());
        httpContext.User.FindFirst("access_token")?.Value.Should().Be("new_access_token");
        _circuitUserContext.RefreshToken.Should().Be("new_refresh_token");
    }
    
    // -------------------------------------------------------------------------
    // SendAsync Failure
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at kallet for å refreshe token til RefreshToken-endepunktet gir oss en feilmelding.
    /// Ingen token ble oppdatert og returnerer original response
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndRefreshesTokenFails_ReturnOriginalResponse()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "user-id"),
                new Claim("access_token", "valid_access_token")], "Cookie"));
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unauthorized
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        
        // Mocker at vi får feilmelding når vi tester å refreshe token. Feil koden her spiller ingen rolle
        // utenom logging
        _tokenRefreshService
            .Setup(x => x.RefreshPairAsync(It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Token refresh feilet")));
        
        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);
        
        // Assert - Sjekker korrekt statuskode, hovedklienten ble kalt kun engang, og RefreshPairAsync ble kalt 1 gang,
        // men ikke ApplyTokenPair
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _mainHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _tokenRefreshService.Verify(x => x.RefreshPairAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once());
        
    }
    
    /// <summary>
    /// Tester at vi returnerer original 401-respons uten å kalle TokenRefreshService
    /// når brukeren ikke har en sub-claim med UserId
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndNoSubClaim_ReturnsOriginalResponse()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext();
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unauthorized
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        
        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _mainHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _tokenRefreshService.Verify(x => x.RefreshPairAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        
    }
    
    /// <summary>
    /// Tester at CloneAsync kloner den originale responsen korrekt
    /// </summary>
    [Fact]
    public async Task SendAsync_RequestHasBody_RetryRequestHasSameBody()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "user-id"),
                new Claim("access_token", "valid_access_token")], "Cookie"));

        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unauthorized og deretter 200 Ok. Bruker en teller for å sikre at vi før 
        // ønskete responser på kallene våre
        int callNumber = 0;
        
        // Vi lagrer HttpRequestMessage-ene til hvert kall i en liste med hjelp av callback for hver response
        var capturedRequests = new List<HttpRequestMessage>();
        
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((httpRequestMessage, _) 
                => capturedRequests.Add(httpRequestMessage))
            .ReturnsAsync(() =>
            {
                callNumber++;
                return callNumber == 1 
                    ? new HttpResponseMessage(HttpStatusCode.Unauthorized) 
                    : new HttpResponseMessage(HttpStatusCode.OK);
            });
        
        // Mocker vellykket token refresh
        var refreshRecord = new RefreshRecord("new_access_token", "new_refresh_token",
            DateTimeOffset.UtcNow);
        _tokenRefreshService
            .Setup(x => x.RefreshPairAsync(It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Success(refreshRecord));
        
        // Oppretter en request body som vi legger til original requesten
        var requestBody = new { Title = "test", Value = 123 };
        var content = JsonContent.Create(requestBody);
        
        // Act
        await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Post, TestEndpoint)
            {
                Content = content
            },
            CancellationToken.None);
        
        // Assert - Sjekker at bodyene er like
        string originalBody = await capturedRequests[0].Content!.ReadAsStringAsync();
        string retryBody = await capturedRequests[1].Content!.ReadAsStringAsync();
        retryBody.Should().Be(originalBody);
    }
    
    /// <summary>
    /// Tester at vi får 401 på første kall, men at CookieValidationEvents allerede har refreshet tokene
    /// på samme request. GetTokenPairAsync returnerer RecentlyRefreshed. Vi sender da den nye responsen 
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndRecentlyRefreshed_RetriesWithFreshTokenFromClaims()
    {
        // Arrange - Oppretter en innlogget bruker med sub-claim
        DefaultHttpContext httpContext = BuildHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", "user-id"),
                new Claim("access_token", "fresh_access_token")], "Cookie"));

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        // Mocker 401 på første kall, 200 på retry
        int callNumber = 0;
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callNumber++;
                return callNumber == 1
                    ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    : new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Mocker at TokenRefreshService returnerer RecentlyRefreshed — CookieValidationEvents
        // har allerede refreshet token på denne requesten
        _tokenRefreshService
            .Setup(x => x.RefreshPairAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.RecentlyRefreshed,
                "Nylig oppdatert")));

        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);

        // Assert - Skal retry og returnere 200, ikke gi opp med 401
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mainHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
    
    /// <summary>
    /// Tester at vi returnerer original 401-respons uten å kalle TokenRefreshService
    /// når brukeren ikke har refresh token cookie i HttpContext
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndNoRefreshTokenCookie_ReturnsOriginalUnauthorizedResponse()
    {
        // Arrange - Bruker med sub-claim men ingen refresh token cookie
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([
                    new Claim("sub", "user-id"),
                    new Claim("access_token", "valid_access_token")
                ], "Cookie"))
        };

        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _tokenRefreshService.Verify(x => x.RefreshPairAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }
}