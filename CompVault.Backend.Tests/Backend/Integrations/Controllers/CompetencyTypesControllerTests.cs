using System.Net;
using System.Net.Http.Json;

using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Backend.Tests.Backend.Features.Auth.Builders;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.DTOs.CompetencyTypes;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace CompVault.Backend.Tests.Backend.Integrations.Controllers;

public class CompetencyTypesControllerTests(
    BackendWebApplicationFactory factory) : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private HttpClient? _authenticatedClient;
    private const string BaseUrl = "/api/competencytypes";

    public async Task InitializeAsync()
    {
        _ = factory.CreateClient();
        factory.EmailServiceMock.Reset();
        await TestDataSeeder.CreateDb(factory.Services);
        // Use Admin role to have write/delete permissions for all tests
        await TestDataSeeder.SeedUserAsync(factory.Services, id: TestConstants.Users.ActiveUserId, role: TestConstants.Roles.Admin);
        _authenticatedClient = await GetAuthenticatedClientAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Hjelper metode som autentiserer via OTP-flyt og returnerer en HttpClient med gyldig token.
    /// </summary>
    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        // Sett opp mock for å fange opp OTP-koden fra email
        string? capturedCode = null;
        factory.EmailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()))
            .Callback<string, EmailBody, CancellationToken>((_, body, _) =>
                capturedCode = body.Subject.Split(": ").Last())
            .ReturnsAsync(Result.Success());

        // Kall RequestOtp for å få en kode generert
        RequestOtpRequest otpRequest = AuthRequestBuilder.CreateRequestOtpRequest();
        await _client.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, otpRequest);

        // Kall VerifyOtp med den fangede koden for å få tokens
        VerifyOtpRequest verifyRequest = AuthRequestBuilder.CreateVerifyOtpRequest(otpCode: capturedCode!);
        HttpResponseMessage verifyResponse = await _client.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, verifyRequest);

        TokenResponse tokens = (await verifyResponse.Content.ReadFromJsonAsync<TokenResponse>())!;

        // Opprett en ny HttpClient med Authorization header
        HttpClient authenticatedClient = factory.CreateClient();
        authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        return authenticatedClient;
    }

    // -------------------------------------------------------------------------
    // GET /api/competencytypes Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetAll krever autentisering.
    /// </summary>
    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tester at GetAll returnerer 200 OK med liste.
    /// </summary>
    [Fact]
    public async Task GetAll_Authenticated_Returns200()
    {
        // Arrange - Lag en type først
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType type = new()
        {
            Name = "Førerkort B",
            IsActive = true
        };
        context.Set<CompetencyType>().Add(type);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _authenticatedClient!.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // GET /api/competencytypes/{id} Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetById returnerer 404 for ikke-eksisterende type.
    /// </summary>
    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _authenticatedClient!.GetAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // POST /api/competencytypes Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at Create returnerer 400 ved duplikat navn.
    /// </summary>
    [Fact]
    public async Task Create_DuplicateName_Returns400()
    {
        // Arrange
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType existingType = new()
        {
            Name = "Førerkort B",
            IsActive = true
        };
        context.Set<CompetencyType>().Add(existingType);
        await context.SaveChangesAsync();

        var request = new CreateCompetencyTypeRequest
        {
            Name = "Førerkort B", // Duplikat navn
            RequiresExpiration = true
        };

        // Act
        HttpResponseMessage response = await _authenticatedClient!.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity); // Validation errors return 422
    }

    /// <summary>
    /// Tester at Create returnerer 201 ved gyldig request.
    /// </summary>
    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        // Arrange
        var request = new CreateCompetencyTypeRequest
        {
            Name = "HMS-kurs",
            Description = "Helse, miljø og sikkerhet",
            Category = "HMS",
            RequiresExpiration = true
        };

        // Act
        HttpResponseMessage response = await _authenticatedClient!.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        CompetencyTypeDto? dto = await response.Content.ReadFromJsonAsync<CompetencyTypeDto>();
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("HMS-kurs");
        dto.IsActive.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // PUT /api/competencytypes/{id} Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at Update returnerer 404 for ikke-eksisterende type.
    /// </summary>
    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateCompetencyTypeRequest { Name = "New Name" };

        // Act
        HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Tester at Update returnerer 400 ved duplikat navn.
    /// </summary>
    [Fact]
    public async Task Update_DuplicateName_Returns400()
    {
        // Arrange
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType existingType = new()
        {
            Name = "Eksisterende Type",
            IsActive = true
        };
        context.Set<CompetencyType>().Add(existingType);

        CompetencyType typeToUpdate = new()
        {
            Name = "Annen Type",
            IsActive = true
        };
        context.Set<CompetencyType>().Add(typeToUpdate);
        await context.SaveChangesAsync();

        var request = new UpdateCompetencyTypeRequest
        {
            Name = "Eksisterende Type" // Prøver å endre til duplikat
        };

        // Act
        HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{typeToUpdate.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity); // Validation errors return 422
    }

    /// <summary>
    /// Tester at Update returnerer 200 ved gyldig request.
    /// </summary>
    [Fact]
    public async Task Update_ValidRequest_Returns200()
    {
        // Arrange
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType type = new()
        {
            Name = "Test Type",
            Description = "Original",
            IsActive = true
        };
        context.Set<CompetencyType>().Add(type);
        await context.SaveChangesAsync();

        var request = new UpdateCompetencyTypeRequest
        {
            Name = "Oppdatert Navn",
            Description = "Oppdatert"
        };

        // Act
        HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{type.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // -------------------------------------------------------------------------
    // DELETE /api/competencytypes/{id} Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at Delete returnerer 404 for ikke-eksisterende type.
    /// </summary>
    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Tester at Delete returnerer 409 når typen har aktive competencies.
    /// </summary>
    [Fact]
    public async Task Delete_HasCompetencies_Returns409()
    {
        // Arrange
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType type = new()
        {
            Name = "Type Med Competencies",
            IsActive = true
        };
        context.Set<CompetencyType>().Add(type);

        Competency competency = new()
        {
            CompetencyTypeId = type.Id,
            UserId = TestConstants.Users.ActiveUserId,
            IssuedDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddDays(100),
            Status = CompetencyStatus.Valid,
            IsActive = true
        };
        context.Set<Competency>().Add(competency);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{type.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Tester at Delete returnerer 204 ved vellykket sletting.
    /// </summary>
    [Fact]
    public async Task Delete_ValidRequest_Returns204()
    {
        // Arrange
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType type = new()
        {
            Name = "Type Utan Competencies",
            IsActive = true
        };
        context.Set<CompetencyType>().Add(type);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{type.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
