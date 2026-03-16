using System.Net;
using System.Net.Http.Json;
using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Shared.Constants;
using CompVault.Shared.Result;
using CompVault.Tests.Backend.Features.Auth.Builders;
using CompVault.Tests.Common;
using CompVault.Tests.Common.Constants;
using FluentAssertions;
using Moq;

namespace CompVault.Tests.Backend.Integrations.Controllers;

/// <summary>
/// IClassFixture sikrer at vi ikke oppretter flere instanser av applikasjonen
/// Siden xUnit-konstruktøren kan ikke være async så oppretter vi to hooks per test:
/// En InitializeAsync som kjører for hver test, og en DisposeAsync for å rydde opp
/// </summary>
public class AuthControllerTests(BackendWebApplicationFactory factory)
    : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
{
    // Oppretter en Httpclient for å sende forespørsler mot endepunktene våre
    private readonly HttpClient _client = factory.CreateClient();
    
    // Initialiserer InMemory-databsen og rydder opp etter AuthController sin kjøring
    public async Task InitializeAsync() => await TestDataSeeder.CreateDbAndSeedUsersAsync(factory.Services);
    public Task DisposeAsync() => Task.CompletedTask;
    
    // -------------------------------------------------------------------------
    // POST /api/auth/request-otp
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester happypath. Sjekker at endepunktet returnerer Ok og EmailService blir kalt
    /// </summary>
    [Fact]
    public async Task RequestOtp_ExistingEmailWithNoExistingCode_Returns200()
    {
        // Arrange
        var request = AuthRequestBuilder.CreateRequestOtpRequest();
        
        // mocker EmailService til å returnere success
        factory.EmailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailBody>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        
        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.EmailServiceMock.Verify(x => x.SendAsync(TestConstants.Users.DefaultEmailForActiveUser,
            It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Once);

    }
}