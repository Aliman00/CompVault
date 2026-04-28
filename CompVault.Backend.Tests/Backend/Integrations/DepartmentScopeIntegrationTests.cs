using System.Security.Claims;
using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Tests.Common;
using CompVault.Shared.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
namespace CompVault.Backend.Tests.Backend.Integrations;

[Collection(nameof(IntegrationTestCollection))]
public class DepartmentScopeIntegrationTests(BackendWebApplicationFactory factory) : IAsyncLifetime
{
    private Department _departmentA = null!;
    private Department _departmentB = null!;
    private Department _subDepartment = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        
        _departmentA = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Avdeling A");
        _departmentB = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Avdeling B");
        _subDepartment = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Underavdeling A",
            parentDepartmentId: _departmentA.Id);
    }
    
    public Task DisposeAsync() => Task.CompletedTask;
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Bygger en DbContext med Interceptoren koblet på og med en autentisert bruker som utfører kallet,
    /// med claim for brukren og en avdeling
    /// </summary>
    /// <param name="departmentId"></param>
    /// <param name="permissions"></param>
    /// <returns></returns>
    private AppDbContext CreateContext(Guid departmentId, params string[] permissions)
    {
        // Legger til en tilfeldig bruker og en avdeling vi har seedet inn
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()), 
            new("department_id", departmentId.ToString())
        };
        
        foreach (string permission in permissions)
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = principal };
        
        // Mocker at HttpContextAccessor returnerer den bygde http-forespørselen med brukeren
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        IServiceScope scope = factory.Services.CreateScope();

        var departmentScope = new DepartmentScopeService(httpContextAccessor.Object, scope.ServiceProvider);
        
        // Kobler på interceptoren for å fange opp operasjoner som skjer mot databasen
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(factory.GetConnectionString())
            .AddInterceptors(new DepartmentScopeSaveChangesInterceptor(scope.ServiceProvider))
            .Options;

        return new AppDbContext(options, departmentScope);
    }
    
    // -------------------------------------------------------------------------
    // Test av query filteret i ModelBuilder for lese-operasjoner
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester oppretting av to brukere i hver sin avdeling og henter alle brukerne i en liste, uten noen permissions.
    /// Sikrer at filteret fungerer sånn at brukere kun kan hente fra sin avdeling uten tilattelseter
    /// </summary>
    [Fact]
    public async Task QueryFilter_UserInDepartmentA_DoesNotSeeUsersInDepartmentB()
    {
        // Arrange
        ApplicationUser userA = await TestDataSeeder.SeedUserAsync(factory.Services, 
            email: "usera@cv.no", departmentId: _departmentA.Id);
        ApplicationUser userB = await TestDataSeeder.SeedUserAsync(factory.Services, 
            email: "userB@cv.no", departmentId: _departmentB.Id);
        
        await using AppDbContext context = CreateContext(_departmentA.Id);
        
        // Act
        List<ApplicationUser> result = await context.Users.ToListAsync();
        
        // Assert
        result.Should().Contain(u => u.Id == userA.Id);
        result.Should().NotContain(u => u.Id == userB.Id);
    }
    
    /// <summary>
    /// Sjekker at vi henter alle brukere med Bypass
    /// </summary>
    [Fact]
    public async Task QueryFilter_UserWithBypass_SeesAllUsers()
    {
        // Arrange - Oppretter to brukere, og bruker A har UsersAll og kan hente alle
        ApplicationUser userA = await TestDataSeeder.SeedUserAsync(factory.Services, 
            email: "usera@cv.no", departmentId: _departmentA.Id);
        ApplicationUser userB = await TestDataSeeder.SeedUserAsync(factory.Services, 
            email: "userB@cv.no", departmentId: _departmentB.Id);
      
        await using AppDbContext context = CreateContext(_departmentA.Id, Permissions.UsersAll);
        
        // Act
        List<ApplicationUser> result = await context.Users.ToListAsync();
        
        // Assert
        result.Should().Contain(u => u.Id == userA.Id);
        result.Should().Contain(u => u.Id == userB.Id);
    }
    
    /// <summary>
    /// Sjekekr at vi henter kun brukeren og brukeren i underavdelingen, men ikke avdelinger vi ikke har tilattelse til
    /// </summary>
    [Fact]
    public async Task QueryFilter_UserWithSubPermission_SeesOnlyAllowedUsers()
    {
        // Arrange - Oppretter to brukere, og bruker A har UsersAll og kan hente alle
        ApplicationUser userA = await TestDataSeeder.SeedUserAsync(factory.Services, 
            email: "usera@cv.no", departmentId: _departmentA.Id);
        ApplicationUser userSubA = await TestDataSeeder.SeedUserAsync(factory.Services, 
            email: "usersuba@cv.no", departmentId: _subDepartment.Id);
        ApplicationUser userB = await TestDataSeeder.SeedUserAsync(factory.Services, 
            email: "userB@cv.no", departmentId: _departmentB.Id);
      
        await using AppDbContext context = CreateContext(_departmentA.Id, Permissions.UsersReadSub);
        
        // Act
        List<ApplicationUser> result = await context.Users.ToListAsync();
        
        // Assert
        result.Should().Contain(u => u.Id == userA.Id);
        result.Should().Contain(u => u.Id == userSubA.Id);
        result.Should().NotContain(u => u.Id == userB.Id);
    }
    
    // -------------------------------------------------------------------------
    // Test av interceptor for skrive/oppdaterings-operasjoner
    // -------------------------------------------------------------------------
    
}