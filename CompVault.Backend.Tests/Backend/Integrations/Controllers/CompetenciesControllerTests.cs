using System.Net;
using System.Net.Http.Json;

using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.DTOs.Competencies;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace CompVault.Backend.Tests.Backend.Integrations.Controllers;

[Collection(nameof(IntegrationTestCollection))]
public class CompetenciesControllerTests(
    BackendWebApplicationFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private HttpClient? _authenticatedClient;
    private const string BaseUrl = "/api/competencies";

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        factory.EmailServiceMock.Reset();
        // Use Admin role to have write/delete permissions for all tests
        await TestDataSeeder.SeedUserAsync(factory.Services, id: TestConstants.Users.ActiveUserId, role: TestConstants.Roles.Admin);
        _authenticatedClient = await TestDataSeeder.CreateAuthenticatedClientAsync(factory);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Authentication Tests - These work without auth
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        HttpResponseMessage response = await _client.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_Unauthenticated_Returns401()
    {
        var nonExistentId = Guid.NewGuid();
        HttpResponseMessage response = await _client.GetAsync($"{BaseUrl}/{nonExistentId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var request = new CreateCompetencyRequest
        {
            UserId = TestConstants.Users.ActiveUserId,
            CompetencyTypeId = Guid.NewGuid(),
            IssuedDate = DateTime.UtcNow.AddDays(-10)
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(BaseUrl, request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Unauthenticated_Returns401()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateCompetencyRequest { Notes = "Test" };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Unauthenticated_Returns401()
    {
        var nonExistentId = Guid.NewGuid();
        HttpResponseMessage response = await _client.DeleteAsync($"{BaseUrl}/{nonExistentId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // Authenticated Tests - Require full OTP authentication flow
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_Authenticated_Returns200()
    {
        HttpResponseMessage response = await _authenticatedClient!.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var nonExistentId = Guid.NewGuid();
        HttpResponseMessage response = await _authenticatedClient!.GetAsync($"{BaseUrl}/{nonExistentId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        // Arrange - Opprett en CompetencyType først
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType type = new()
        {
            Name = "Førerkort B",
            RequiresExpiration = true,
            IsActive = true
        };
        context.Set<CompetencyType>().Add(type);
        await context.SaveChangesAsync();

        var request = new CreateCompetencyRequest
        {
            UserId = TestConstants.Users.ActiveUserId,
            CompetencyTypeId = type.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddDays(365) // Required because RequiresExpiration = true
        };

        // Act
        HttpResponseMessage response = await _authenticatedClient!.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_InactiveType_Returns400()
    {
        // Arrange - Opprett en inaktiv CompetencyType
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType type = new()
        {
            Name = "Inaktiv Type",
            RequiresExpiration = false, // Not required since it's inactive anyway
            IsActive = false
        };
        context.Set<CompetencyType>().Add(type);
        await context.SaveChangesAsync();

        var request = new CreateCompetencyRequest
        {
            UserId = TestConstants.Users.ActiveUserId,
            CompetencyTypeId = type.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-10)
            // No ExpiryDate needed since RequiresExpiration = false
        };

        // Act
        HttpResponseMessage response = await _authenticatedClient!.PostAsJsonAsync(BaseUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity); // Validation errors return 422
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateCompetencyRequest { Notes = "Test" };

        HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var nonExistentId = Guid.NewGuid();

        HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{nonExistentId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ValidRequest_Returns204()
    {
        // Arrange - Opprett en CompetencyType og en Competency
        IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        CompetencyType type = new()
        {
            Name = "Førerkort B",
            RequiresExpiration = true,
            IsActive = true
        };
        context.Set<CompetencyType>().Add(type);

        Competency competency = new()
        {
            CompetencyTypeId = type.Id,
            UserId = TestConstants.Users.ActiveUserId,
            IssuedDate = DateTime.UtcNow.AddDays(-10),
            IsActive = true
        };
        context.Set<Competency>().Add(competency);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{competency.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}