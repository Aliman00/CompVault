using System.Net;
using System.Net.Http.Json;

using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Departments;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Backend.Tests.Backend.Integrations.Controllers;

public class DepartmentsControllerTests(
    BackendWebApplicationFactory factory) : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private HttpClient? _authenticatedClient;
    private const string BaseUrl = "/api/departments";

    public async Task InitializeAsync()
    {
        await TestDataSeeder.CreateDb(factory.Services);

        // Seed admin user with all permissions for happy path tests
        await TestDataSeeder.SeedUserAsync(
            factory.Services,
            id: TestConstants.Users.ActiveUserId,
            role: TestConstants.Roles.Admin);

        // Create authenticated client AFTER permissions are granted
        _authenticatedClient = await TestDataSeeder.CreateAuthenticatedClientAsync(factory, TestConstants.Users.ActiveUserId);
    }

    public Task DisposeAsync()
    {
        _authenticatedClient?.Dispose();
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a user with only DepartmentsRead permission (no write/delete).
    /// </summary>
    private async Task<HttpClient> CreateReadOnlyAuthenticatedClientAsync()
    {
        var readOnlyUserId = Guid.NewGuid();
        string readOnlyEmail = "readonly@compvault.test";

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create a read-only role if it doesn't exist
        ApplicationRole? readOnlyRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "ReadOnly");
        if (readOnlyRole == null)
        {
            readOnlyRole = new CompVault.Backend.Domain.Entities.Identity.ApplicationRole
            {
                Name = "ReadOnly",
                NormalizedName = "READONLY"
            };
            context.Roles.Add(readOnlyRole);
            await context.SaveChangesAsync();
        }

        // Grant only DepartmentsRead permission
        Permission? deptReadPermission = await context.Permissions
            .FirstOrDefaultAsync(p => p.Name == Permissions.DepartmentsRead);
        if (deptReadPermission != null)
        {
            bool exists = await context.RolePermissions.AnyAsync(
                rp => rp.RoleId == readOnlyRole.Id && rp.PermissionId == deptReadPermission.Id);
            if (!exists)
            {
                context.RolePermissions.Add(new CompVault.Backend.Domain.Entities.Identity.RolePermission
                {
                    RoleId = readOnlyRole.Id,
                    PermissionId = deptReadPermission.Id,
                    GrantedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }

        // Seed the read-only user
        await TestDataSeeder.SeedUserAsync(
            factory.Services,
            id: readOnlyUserId,
            email: readOnlyEmail,
            role: "ReadOnly");

        // Create authenticated client for this user
        return await TestDataSeeder.CreateAuthenticatedClientAsync(factory, readOnlyUserId);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_Returns401Unauthorized()
    {
        HttpResponseMessage response = await _client.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithoutAuth_Returns401Unauthorized()
    {
        var request = new CreateDepartmentRequest { Name = "Test" };
        HttpResponseMessage response = await _client.PostAsJsonAsync(BaseUrl, request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithoutAuth_Returns401Unauthorized()
    {
        var request = new UpdateDepartmentRequest { Name = "Test" };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithoutAuth_Returns401Unauthorized()
    {
        HttpResponseMessage response = await _client.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithoutWritePermission_Returns403Forbidden()
    {
        HttpClient readOnlyClient = await CreateReadOnlyAuthenticatedClientAsync();
        var request = new CreateDepartmentRequest { Name = "Unauthorized Create" };

        HttpResponseMessage response = await readOnlyClient.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithoutWritePermission_Returns403Forbidden()
    {
        HttpClient readOnlyClient = await CreateReadOnlyAuthenticatedClientAsync();
        var request = new UpdateDepartmentRequest { Name = "Unauthorized Update" };

        HttpResponseMessage response = await readOnlyClient.PutAsJsonAsync($"{BaseUrl}/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithoutDeletePermission_Returns403Forbidden()
    {
        HttpClient readOnlyClient = await CreateReadOnlyAuthenticatedClientAsync();

        HttpResponseMessage response = await readOnlyClient.DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_Authenticated_Returns200Ok()
    {
        HttpResponseMessage response = await _authenticatedClient!.GetAsync(BaseUrl);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_WithValidId_Returns200Ok()
    {
        // Arrange - create a department first
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Department department = TestDataFactory.CreateDepartment(name: "Test Department");
        context.Set<Department>().Add(department);
        await context.SaveChangesAsync();

        try
        {
            // Act
            HttpResponseMessage response = await _authenticatedClient!.GetAsync($"{BaseUrl}/{department.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            DepartmentDto? dto = await response.Content.ReadFromJsonAsync<DepartmentDto>();
            dto.Should().NotBeNull();
            dto!.Name.Should().Be("Test Department");
        }
        finally
        {
            // Cleanup
            context.Set<Department>().Remove(department);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Create_WithValidData_Returns201Created()
    {
        var request = new CreateDepartmentRequest
        {
            Name = "New Department",
            Description = "Test description"
        };

        HttpResponseMessage response = await _authenticatedClient!.PostAsJsonAsync(BaseUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        DepartmentDto? dto = await response.Content.ReadFromJsonAsync<DepartmentDto>();
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("New Department");
        dto.Id.Should().NotBeEmpty();

        // Cleanup
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Department? department = await context.Set<Department>().FindAsync(dto.Id);
        if (department != null)
        {
            context.Set<Department>().Remove(department);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Update_WithValidData_Returns200Ok()
    {
        // Arrange - create a department first
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Department department = TestDataFactory.CreateDepartment(name: "Original Name");
        context.Set<Department>().Add(department);
        await context.SaveChangesAsync();

        try
        {
            var request = new UpdateDepartmentRequest
            {
                Name = "Updated Name",
                Description = "Updated description"
            };

            // Act
            HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{department.Id}", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            DepartmentDto? dto = await response.Content.ReadFromJsonAsync<DepartmentDto>();
            dto.Should().NotBeNull();
            dto!.Name.Should().Be("Updated Name");
        }
        finally
        {
            // Cleanup
            context.Set<Department>().Remove(department);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Delete_WithValidId_Returns204NoContent()
    {
        // Arrange - create a department
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Department department = TestDataFactory.CreateDepartment(name: "To Delete");
        context.Set<Department>().Add(department);
        await context.SaveChangesAsync();

        // Act
        HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{department.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Cleanup
        context.Set<Department>().Remove(department);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404NotFound()
    {
        var nonExistentId = Guid.NewGuid();

        HttpResponseMessage response = await _authenticatedClient!.GetAsync($"{BaseUrl}/{nonExistentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithNonExistentId_Returns404NotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateDepartmentRequest { Name = "Test" };

        HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{nonExistentId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithCircularReference_Returns422UnprocessableEntity()
    {
        // Arrange - create A -> B hierarchy
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Department deptA = TestDataFactory.CreateDepartment(name: "A");
        Department deptB = TestDataFactory.CreateDepartment(name: "B", parentDepartmentId: deptA.Id);

        context.Set<Department>().AddRange(deptA, deptB);
        await context.SaveChangesAsync();

        try
        {
            // Try to set B as parent of A (circular: A -> B -> A)
            var request = new UpdateDepartmentRequest
            {
                ParentDepartmentId = deptB.Id
            };

            // Act
            HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{deptA.Id}", request);

            // Assert - should return 422 (Validation error), not 409
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
        finally
        {
            // Cleanup - clear navigation properties to avoid EF Core issues
            deptA.ParentDepartmentId = null;
            deptB.ParentDepartmentId = null;
            context.Set<Department>().RemoveRange(deptA, deptB);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Update_WithSelfAsParent_Returns422UnprocessableEntity()
    {
        // Arrange - create a department
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Department department = TestDataFactory.CreateDepartment(name: "Self Ref");
        context.Set<Department>().Add(department);
        await context.SaveChangesAsync();

        try
        {
            // Try to set itself as parent
            var request = new UpdateDepartmentRequest
            {
                ParentDepartmentId = department.Id
            };

            // Act
            HttpResponseMessage response = await _authenticatedClient!.PutAsJsonAsync($"{BaseUrl}/{department.Id}", request);

            // Assert - should return 422 (Validation error)
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
        finally
        {
            // Cleanup
            department.ParentDepartmentId = null;
            context.Set<Department>().Remove(department);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Delete_WithSubDepartments_Returns409Conflict()
    {
        // Arrange - create parent with child
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Department parent = TestDataFactory.CreateDepartment(name: "Parent");
        Department child = TestDataFactory.CreateDepartment(name: "Child", parentDepartmentId: parent.Id);

        context.Set<Department>().AddRange(parent, child);
        await context.SaveChangesAsync();

        try
        {
            // Act - try to delete parent
            HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{parent.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            // Cleanup
            child.ParentDepartmentId = null;
            context.Set<Department>().RemoveRange(parent, child);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Delete_WithMembers_Returns409Conflict()
    {
        // Arrange - create a department with a member (user)
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Department department = TestDataFactory.CreateDepartment(name: "With Members");

        context.Set<Department>().Add(department);
        await context.SaveChangesAsync();

        // Create a user assigned to this department
        ApplicationUser user = TestDataFactory.CreateApplicationUser(email: "member@compvault.test");
        user.DepartmentId = department.Id;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        try
        {
            // Act - try to delete department with members
            HttpResponseMessage response = await _authenticatedClient!.DeleteAsync($"{BaseUrl}/{department.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            // Cleanup
            context.Users.Remove(user);
            context.Set<Department>().Remove(department);
            await context.SaveChangesAsync();
        }
    }
}