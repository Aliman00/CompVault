using CompVault.Frontend.Common.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

using Moq;

namespace CompVault.Tests.Frontend.Common.Http;

public class AccessTokenHandler
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly AuthSettings _authSettings;
    // Vi mocker selve handleren av HttpClienten
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly Mock<HttpMessageHandler> _authClientHandlerMock;

    private readonly AccessTokenHandler _sut;
    
    public AccessTokenHandler()
    {

    
    
    }
}