using System.Net;
using System.Net.Http.Json;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http.Models;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
namespace CompVault.Frontend.Tests.Frontend.Common.Services;

public class TokenRefreshServiceTests
{
    private readonly TokenRefreshService _sut;
    
    private readonly Mock<HttpMessageHandler> _authClientHandlerMock;

    private const string SendAsync = "SendAsync";
    private const string BaseAddress = "https://backend";
    
    private readonly string _userId = Guid.NewGuid().ToString();

    public TokenRefreshServiceTests()
    {
        var authSettings = new AuthSettings { ValidationIntervalMinutes = 10 };
        
        Mock<ILogger<TokenRefreshService>> loggerMock = new();
        
        // Mocker HttpClientFactory til å bruke Auth-klienten
        _authClientHandlerMock = new Mock<HttpMessageHandler>();
        var authHttpClient = new HttpClient(_authClientHandlerMock.Object)
        {
            BaseAddress = new Uri(BaseAddress)
        };
        
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(BackendApiSettings.AuthClientName))
            .Returns(authHttpClient);

        _sut = new TokenRefreshService(
            httpClientFactoryMock.Object,
            authSettings,
            loggerMock.Object);
    }
    
    // -------------------------------------------------------------------------
    // RefreshPairAsync tester
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at vi returnerer Failure og NotFound når det ikke er eksisterende refresh token
    /// </summary>
    [Fact]
    public async Task RefreshPairAsync_NoRefreshTokenCookie_ReturnsNotFound()
    {   
        // Act
        Result<RefreshRecord> result = await _sut.RefreshPairAsync(_userId ,string.Empty);
        
        // Assert - Sjekker at det er vi får NotFound og at vi aldri prøver å refreshe token
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be(ErrorCode.NotFound);
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Never(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
    
    /// <summary>
    /// Tester at vi får AccountInactive ved ErrorCode = UserInactive ved refresh request til backend
    /// </summary>
    [Fact]
    public async Task RefreshPairAsync_AccountInactive_ReturnsUnauthorized()
    {
        // Arrange - HttpContext med refresh token cookie
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
        Result<RefreshRecord> result = await _sut.RefreshPairAsync(_userId, "valid_token");
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be(ErrorCode.Unauthorized);
    }
    
    /// <summary>
    /// Tester at cooldown i RefreshPariAsync fungerer. Utfører først et kall, deretter etter kall til og vi sjekker
    /// at flere kall ikke sender ny refresh token
    /// </summary>
    [Fact]
    public async Task RefreshPairAsync_RecentlyRefreshed_ReturnsRecentlyRefreshedAndOnlyOneRefresh()
    {
        // Arrange - Første kall setter LastRefreshed
        _authClientHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse
                {
                    AccessToken = "new_access_token",
                    RefreshToken = "new_refresh_token"
                })
            }));
        
        await _sut.RefreshPairAsync(_userId, "valid_token");
        
        // Act - Andre kall mens første kall satt oss på cooldown
        Result<RefreshRecord> result = await _sut.RefreshPairAsync(_userId, "valid_token");
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be(ErrorCode.RecentlyRefreshed);
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Once(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
    
    /// <summary>
    /// Tester at vi ikke får cooldown på forskjellige brukere når de refresher token
    /// </summary>
    [Fact]
    public async Task RefreshPairAsync_DifferentUsers_RefreshesIndependently()
    {
        // Arrange - Setter opp testen til å utføre refresh med to brukere
        string otherUserId = Guid.NewGuid().ToString();
        
        // Bruker factory til å opprette en instanse, siden de er single use og konsumeres etter bruk
        _authClientHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse
                {
                    AccessToken = "new_access_token",
                    RefreshToken = "new_refresh_token"
                })
            }));
        
        
        // Første bruker refresher
        await _sut.RefreshPairAsync(_userId ,"valid_token");
        
        // Act - Andre bruker refresher
        Result<RefreshRecord> result = await _sut.RefreshPairAsync(otherUserId ,"valid_token");
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        _authClientHandlerMock
            .Protected()
            .Verify(SendAsync, Times.Exactly(2), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
    
    /// <summary>
    /// Tester at vi feiler åpent, brukeren forblir innlogget og returnerer Unknown når backend returnerer
    /// en annen feil enn AccountInactive. Kan komme av pga server nede
    /// </summary>
    [Fact]
    public async Task RefreshPairAsync_BackendReturnsOtherError_ReturnsUnknown()
    {
        // Arrange
        _authClientHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        // Act
        Result<RefreshRecord> result = await _sut.RefreshPairAsync(_userId, "valid_token");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error?.Code.Should().Be(ErrorCode.Unknown);
    }
    
    /// <summary>
    /// Tester happy path ved at vi returnerer RefreshRecord med riktige tokens
    /// </summary>
    [Fact]
    public async Task RefreshPairAsync_ValidRefreshToken_ReturnsRefreshRecord()
    {
        // Arrange - Bruker factory til å opprette en instanse, siden de er single use og konsumeres etter bruk
        _authClientHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(SendAsync, ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse
                {
                    AccessToken = "new_access_token",
                    RefreshToken = "new_refresh_token"
                })
            }));
        
        // Act
        Result<RefreshRecord> result = await _sut.RefreshPairAsync(_userId, "valid_token");
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new_access_token");
        result.Value!.RefreshToken.Should().Be("new_refresh_token");
        result.Value!.RefreshedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}