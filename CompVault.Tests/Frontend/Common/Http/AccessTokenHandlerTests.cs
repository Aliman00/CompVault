using System.Net;
using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;


namespace CompVault.Tests.Frontend.Common.Http;

public class AccessTokenHandlerTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly AuthSettings _authSettings;
    // Vi mocker selve handleren av HttpClienten - for main og auth
    private readonly Mock<HttpMessageHandler> _mainHandlerMock;
    private readonly Mock<HttpMessageHandler> _authClientHandlerMock;

    private readonly AccessTokenHandler _sut;

    private const  string BaseAddress = "https://backend";
    private const  string TestEndpoint = $"{BaseAddress}/api/test";
    
    public AccessTokenHandlerTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _envMock = new Mock<IWebHostEnvironment>();
        _authSettings = new AuthSettings { CookieExpireDays = 7 };
        Mock<ILogger<AccessTokenHandler>> loggerMock = new();
        
        // mocker base.SendAsync - hva backend svarer på vanlige API-kall
        _mainHandlerMock = new Mock<HttpMessageHandler>();
        
        // mocker auth klienter som ikke har AccessTokenHandler påkoblet
        _authClientHandlerMock = new Mock<HttpMessageHandler>();
        var authHttpClient = new HttpClient(_authClientHandlerMock.Object)
        {
            BaseAddress = new Uri("https//backend")
        };
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(BackendApiSettings.AuthClientName))
            .Returns(authHttpClient);
        
        _envMock
            .Setup(x => x.EnvironmentName)
            .Returns(Environments.Development);

        _sut = new AccessTokenHandler(
            _httpContextAccessorMock.Object,
            _httpClientFactoryMock.Object,
            _envMock.Object,
            _authSettings,
            loggerMock.Object);

        _sut.InnerHandler = _mainHandlerMock.Object;

    }
    
    [Fact]
    public async Task SendAsync_StatusCodeIsNot401_ReturnsResponse()
    {
        // Arrange
        // Oppretter en claim med gyldig token, lager en ClaimsIdentity med den og bruker den til å sette
        // en bruker med gydlig token i HttpContext
        var claim = new Claim("access_token", "valid_access_token");
        var identity = new ClaimsIdentity([claim], authenticationType: "Cookie");
        
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(identity);
        
        _httpContextAccessorMock
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
        
        // Mocker en HttpResponse med Ok 200 fra backend. Den er protected, så vi bruker reflection
        _mainHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
        
        // Act
        HttpResponseMessage response = await new HttpMessageInvoker(_sut)
            .SendAsync(new HttpRequestMessage(HttpMethod.Get, TestEndpoint), CancellationToken.None);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _authClientHandlerMock
            .Protected()
            .Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
}