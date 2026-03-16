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
    
    // Initialiserer InMemory-databsen og rydder opp databasen før AuthController kjører
    public async Task InitializeAsync()
    {
        factory.EmailServiceMock.Reset(); // Resetter mocken for å sikre at EmailService resettes mellom kjøringer
        await TestDataSeeder.CreateDbAndSeedUsersAsync(factory.Services);
    }
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
    
    /// <summary>
    /// Tester at vi får Ok når eposten ikke eksisterer. Bruker får samme resulatet for å ikke avsløre om brukeren
    /// eksisterer
    /// </summary>
    [Fact]
    public async Task RequestOtp_UnknownEmail_Returns200()
    {
        // Arrange
        var request = AuthRequestBuilder.CreateRequestOtpRequest(email: "eksisterer@ikke.se");
        
        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.EmailServiceMock.Verify(x => x.SendAsync(TestConstants.Users.DefaultEmailForActiveUser,
            It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Never);

    }
    
    /// <summary>
    /// Tester 2 kall etter hverandre for å sikre at det blir opprettet kun en kode, og at vi får 200 Ok når Error
    /// er OtpCooldown
    /// </summary>
    [Fact]
    public async Task RequestOtp_OtpCooldown_Returns200()
    {
        // Arrange
        var request = AuthRequestBuilder.CreateRequestOtpRequest();
        
        // mocker EmailService til å returnere success
        factory.EmailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailBody>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        
        // Act - Utfører to kall til samme endepunkt
        await _client.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, request);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, request);
        
        // Assert - Sjekker at Result er 200 Ok og at vi kaller EmailService kun engang, ikke to
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.EmailServiceMock.Verify(x => x.SendAsync(TestConstants.Users.DefaultEmailForActiveUser,
            It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    
}