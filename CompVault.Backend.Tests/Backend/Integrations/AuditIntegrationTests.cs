using System.Net.Http.Json;

using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Enums;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Backend.Tests.Backend.Integrations;

[Collection(nameof(IntegrationTestCollection))]
public class AuditIntegrationTests : IAsyncLifetime
{
    private readonly BackendWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private AppDbContext _dbContext = null!;
    private HttpClient _authenticatedClient = null!;
    private ApplicationUser _adminUser = null!;

    public AuditIntegrationTests(BackendWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _dbContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

        _adminUser = await TestDataSeeder.SeedUserAsync(
            _factory.Services, role: TestConstants.Roles.Admin);

        _authenticatedClient = await TestDataSeeder.CreateAuthenticatedClientAsync(
            _factory, _adminUser.Id);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Opprett kompetanse → verifiser AuditLog-entry
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateCompetency_CreatesAuditLogEntry()
    {
        // Arrange — opprett CompetencyType
        var competencyType = new CompetencyType
        {
            Id = Guid.NewGuid(),
            Name = "HMS-kurs",
            Category = "HMS",
            RequiresExpiration = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CompetencyTypes.Add(competencyType);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Opprett competency direkte i DB for å unngå API-kall
        var competency = new Competency
        {
            Id = Guid.NewGuid(),
            UserId = _adminUser.Id,
            CompetencyTypeId = competencyType.Id,
            Status = CompetencyStatus.Valid,
            IssuedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Competencies.Add(competency);
        await _dbContext.SaveChangesAsync();

        // Assert — verify AuditLog was created
        AuditLog? auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == competency.Id && a.Action == "competency.create");

        auditLog.Should().NotBeNull();
        auditLog!.EntityType.Should().Be("Competency");
    }

    // -------------------------------------------------------------------------
    // Soft-delete bruker → verifiser AuditLog
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SoftDeleteUser_CreatesAuditLogEntry()
    {
        // Arrange — soft-delete a user
        ApplicationUser user = await TestDataSeeder.SeedUserAsync(
            _factory.Services,
            email: "delete-test@example.com",
            role: TestConstants.Roles.Default);

        _dbContext.ChangeTracker.Clear();

        ApplicationUser? toDelete = await _dbContext.Users.FindAsync(user.Id);
        toDelete!.DeletedAt = DateTime.UtcNow;
        toDelete.IsActive = false;

        await _dbContext.SaveChangesAsync();

        // Assert
        AuditLog? auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == user.Id && a.Action == "application_user.delete");

        auditLog.Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // GET /api/audit-log returnerer paginert resultat
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAuditLog_ReturnsPagedResult()
    {
        // Act
        var response = await _authenticatedClient.GetAsync(ApiRoutes.Audit.Base);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        PagedResult<AuditLogDto>? result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    // -------------------------------------------------------------------------
    // Filter på action fungerer
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAuditLog_FilterByAction_ReturnsMatchingEntries()
    {
        // Arrange — opprett en entitet for å generere en audit-entry
        var dept = new CompVault.Backend.Domain.Entities.Departments.Department
        {
            Id = Guid.NewGuid(),
            Name = "Test Dept " + Guid.NewGuid().ToString("N")[..8],
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Departments.Add(dept);
        await _dbContext.SaveChangesAsync();

        // Act — filter på department.create
        var response = await _authenticatedClient.GetAsync(
            $"{ApiRoutes.Audit.Base}?action=department.create");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        PagedResult<AuditLogDto>? result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().Contain(a => a.Action == "department.create");
    }

    // -------------------------------------------------------------------------
    // Uautorisert → 401
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAuditLog_Unauthenticated_Returns401()
    {
        // Act — use unauthenticated client
        var response = await _client.GetAsync(ApiRoutes.Audit.Base);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    // -------------------------------------------------------------------------
    // GET /api/audit-log med entityType-filter
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAuditLog_FilterByEntityType_ReturnsMatchingEntries()
    {
        // Act
        var response = await _authenticatedClient.GetAsync(
            $"{ApiRoutes.Audit.Base}?entityType=Department");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        PagedResult<AuditLogDto>? result = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
        result.Should().NotBeNull();
        if (result!.Items.Count > 0)
        {
            result.Items.Should().OnlyContain(a => a.EntityType == "Department");
        }
    }
}