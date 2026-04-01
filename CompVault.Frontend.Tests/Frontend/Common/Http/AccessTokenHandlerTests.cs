using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http;
using CompVault.Shared.DTOs.Auth;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;

namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class AccessTokenHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    // Vi mocker selve handleren av HttpClienten - for main og auth
    private readonly Mock<HttpMessageHandler> _mainHandlerMock;
    private readonly Mock<HttpMessageHandler> _authClientHandlerMock;

    private readonly AccessTokenHandler _sut;
    
    private const string SendAsync = "SendAsync";
    private const string BaseAddress = "https://backend";
    private const string TestEndpoint = $"{BaseAddress}/api/test";
    
    public AccessTokenHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var envMock = new Mock<IWebHostEnvironment>();
        var authSettings = new AuthSettings { CookieExpireDays = 7 };
        Mock<ILogger<AccessTokenHandler>> loggerMock = new();
        
        // mocker base.SendAsync - hva backend svarer på vanlige API-kall
        _mainHandlerMock = new Mock<HttpMessageHandler>();
        
        // mocker auth klienter som ikke har AccessTokenHandler påkoblet
        _authClientHandlerMock = new Mock<HttpMessageHandler>();
        var authHttpClient = new HttpClient(_authClientHandlerMock.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };
        httpClientFactoryMock
            .Setup(x => x.CreateClient(BackendApiSettings.AuthClientName))
            .Returns(authHttpClient);
        
        envMock
            .Setup(x => x.EnvironmentName)
            .Returns(Environments.Development);

        _sut = new AccessTokenHandler(
            _httpContextAccessorMock.Object,
            httpClientFactoryMock.Object,
            envMock.Object,
            authSettings,
            loggerMock.Object);

        _sut.InnerHandler = _mainHandlerMock.Object;

    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    // Oppretter innlogget bruker, ved å bygge en claim med gyldig token, lager en ClaimsIdentity med den
    // og bruker den til å sette en bruker med gydlig token i HttpContext-en vi returnerer. Med eller uten gyldig
    // refresh token
    private DefaultHttpContext BuildHttpContext(bool setRefreshToken = false)
    {
        var accessTokenClaim = new Claim("access_token", "valid_access_token");
        var claimsIdentity = new ClaimsIdentity([accessTokenClaim], "Cookie");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        
        if (setRefreshToken)
            httpContext.Request.Headers.Append("Cookie", "refreshToken=valid_access_token");

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
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Never(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
    
    /// <summary>
    /// tester at vi får 401 på første, men at vi vellykket refresher token og at den klonede responsen gir 200 Ok
    /// Testen sjekker at klientene blir kalt korrekt antall ganger
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndValidRefreshToken_RefreshesTokenAndReturnsResponse()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext(true);
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unathorized og deretter 200 Ok. Vi bruker en teller for å kunne sikre
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
        
        // Mocker klienten som brukes i TryRefreshASync til å få 200 Ok med nye tokens
        _authClientHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token"
                })
            });
        
        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);
        
        // Assert - Sjekker korrekt statuskode og at klientene ble kalt riktig antall gangner. Main klient 2 ganger,
        // og auth klient 1 gang for refresh
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mainHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        
    }
    
    // -------------------------------------------------------------------------
    // SendAsync Failure
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at kallet for å refreshe token til RefreshToken-endepunktet gir oss en feilmelding.
    /// Ingen token oppdaterte og retunrere original response
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndRefreshesTokenFails_ReturnOriginalResponse()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext(true);
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unathorized
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        
        // Mocker klienten som brukes i TryRefreshASync til å få Forbidden. Feil koden her spiller ingen rolle
        // utenom logging
        _authClientHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden));
        
        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);
        
        // Assert - Sjekker korrekt statuskode og at klientene ble kalt riktig antall gangner - 1 gang hver
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _mainHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        
    }
    
    /// <summary>
    /// Tester at vi ikke har gyldig refresh token og at det er en early return før AuthClient blir kallet
    /// </summary>
    [Fact]
    public async Task SendAsync_StatusCodeIs401AndNoRefreshToken_ReturnsOriginalResponse()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext();
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unathorized
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
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Never(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        
    }
    
    /// <summary>
    /// Tester at CloneAsync kloner den originale responsen korrekt
    /// </summary>
    [Fact]
    public async Task SendAsync_RequestHasBody_RetryRequestHasSameBody()
    {
        // Arrange - Oppretter en innlogget bruker
        DefaultHttpContext httpContext = BuildHttpContext(true);
        
        // Mocker at HttpContextAccessor returnerer vår HttpContext
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker først at vi får 401 Unathorized og deretter 200 Ok. Bruker en teller for å sikre at vi før 
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
        
        // Mocker klienten som brukes i TryRefreshASync til å få 200 Ok med nye tokens
        _authClientHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse
                {
                    AccessToken = "access-token",
                    RefreshToken = "refresh-token"
                })
            });
        
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
}