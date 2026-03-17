using System.Net;
using System.Net.Http.Json;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using CompVault.Tests.Backend.Features.Auth.Builders;
using CompVault.Tests.Common;
using CompVault.Tests.Common.Constants;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        await TestDataSeeder.CreateDb(factory.Services);
        await TestDataSeeder.SeedUserAsync(factory.Services, // Seeder en aktiv bruker
            id: TestConstants.Users.ActiveUserId);
        await TestDataSeeder.SeedUserAsync(factory.Services, // Seeder en inaktiv bruker
            id: TestConstants.Users.InactiveUserId,
            email: TestConstants.Users.DefaultEmailForInactiveUser,
            deletedAt: DateTime.UtcNow);
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

    // -------------------------------------------------------------------------
    // POST /api/auth/verify-otp
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifiserer at innsendt Otp er korrekt og at vi 200 OK med LoginResponse
    /// </summary>
    [Fact]
    public async Task VerifyOtp_OtpIsCorrect_Returns200()
    {
        // Arrange - seeder en Otp-kode i databasen til Default bruker
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services);
        var request = AuthRequestBuilder.CreateVerifyOtpRequest();

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request);

        // Assert - Sjekker at Result er 200 Ok og sjekker alle egenskapene på RefreshTokenResponse
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Tester at bruker sender inn en kode som ikke er korrekt, og verifiserer at FailedAttempts har økt med 1
    /// </summary>
    [Fact]
    public async Task VerifyOtp_InvalidCode_Returns401()
    {
        // Arrange - seeder en Otp-kode i databasen med annen kode enn requestens kode
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services);
        var request = AuthRequestBuilder.CreateVerifyOtpRequest(otpCode: "012345");

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request);

        // Assert - Sjekker at Result er 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Henter en Otp-kode for å verifisere at FailedAttempts har blitt økt fra 0 til 1
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var otpCode = await context.Set<OtpCode>()
            .FirstOrDefaultAsync(x => x.UserId == TestConstants.Users.ActiveUserId);

        otpCode!.FailedAttempts.Should().Be(1);
    }

    /// <summary>
    /// Tester at maks forsøk er nådd og vi får en 429 
    /// </summary>
    [Fact]
    public async Task VerifyOtp_MaxAttemptsExceeded_Returns429()
    {
        // Arrange - seeder en Otp-kode i databasen med annen kode enn requestens kode
        await TestDataSeeder.SeedOtpCodeAsync(factory.Services, failedAttempts: 3);
        var request = AuthRequestBuilder.CreateVerifyOtpRequest(otpCode: "012345");

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request);

        // Assert - Sjekker at Result er 429 TooManyRequests
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Tester at innsendt epost i request ikke tilhører en bruker i databasen
    /// </summary>
    [Fact]
    public async Task VerifyOtp_InvalidUser_Returns401()
    {
        // Arrange - Sender en request med en epost som ikke tilhører en bruker
        var request = AuthRequestBuilder.CreateVerifyOtpRequest(email: "eksisterer@ikke.se");

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request);

        // Assert - Sjekker at Result er 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tester at bruker uten aktiv OTP-kode returnerer 401
    /// </summary>
    [Fact]
    public async Task VerifyOtp_NoActiveOtpCode_Returns401()
    {
        // Arrange - Sender en request med en epost som ikke tilhører en bruker
        var request = AuthRequestBuilder.CreateVerifyOtpRequest();

        // Act
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request);

        // Assert - Sjekker at Result er 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/request-otp → POST /api/auth/verify-otp (end-to-end)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester ende-til-ende mellom RequestOtp og VerifyOtp. Sikrer at koden blir opprettet korrekt og verifisert
    /// korrekt.
    /// </summary>
    [Fact]
    public async Task RequestOtp_ThenVerifyOtp_Returns200WithTokens()
    {
        // Arrange - Oppretter en OtpRequest
        var requestOtpRequest = AuthRequestBuilder.CreateRequestOtpRequest();

        // mocker EmailService slik at iv kan fange opp koden som ligger i Subject på EmailBody
        string? capturedCode = null;
        factory.EmailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailBody>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, EmailBody, CancellationToken>((_, body, _) => // Koden skal alltid ligge etter ":"
                capturedCode = body.Subject.Split(": ").Last())
            .ReturnsAsync(Result.Success());

        // Act - Utfører et kall til RequestOtp først, lager en response med koden og kalelr deretter
        // VerifyOtp
        await _client.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, requestOtpRequest);
        var verifyOtpRequest = AuthRequestBuilder.CreateVerifyOtpRequest(otpCode: capturedCode!);
        var response = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, verifyOtpRequest);

        // Assert - Sjekker at StatusCode er 200 Ok og at det er opprettet en RefreshTokenResponse
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }
}
