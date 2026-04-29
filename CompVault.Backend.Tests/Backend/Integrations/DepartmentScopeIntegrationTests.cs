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
        IServiceScope scope = factory.Services.CreateScope();
    
        IHttpContextAccessor httpContextAccessor = TestDataSeeder.CreateHttpContextAccessor(departmentId, permissions);

        var departmentScope = new DepartmentScopeService(httpContextAccessor, scope.ServiceProvider);
        
        // Vi overstyrer DepartmentScope service siden vi har implementert BypassDepartmentScope
        // i WebAppFactory for andre tester. Vi må teste med riktig IDepartmentScopeService
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IDepartmentScopeService)))
            .Returns(departmentScope);
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IHttpContextAccessor)))
            .Returns(httpContextAccessor);
        
        // Kobler på interceptoren for å fange opp operasjoner som skjer mot databasen
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(factory.GetConnectionString())
            .AddInterceptors(new DepartmentScopeSaveChangesInterceptor(serviceProviderMock.Object))
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
    /// <summary>
    /// Tester at en bruker får lov til å opprette en bruker i egen avdeling. Policy 'user:write' sikrer
    /// riktig tilattelse på endepunktet, mens interceptor sikrer at avdelingen må være riktig
    /// </summary>
    [Fact]
    public async Task Interceptor_CreateUserInAllowedDepartment_SavesSuccessfully()
    {
        ApplicationUser user = TestDataFactory.CreateApplicationUser(email: "usera@cv.no", 
            departmentId: _departmentA.Id);
        
        await using AppDbContext context = CreateContext(_departmentA.Id);
        context.Users.Add(user);
        
        // Act and assert - Utfører lagring og sjekker at det ikke blir kastet en feil
        await context.Invoking(c => c.SaveChangesAsync())
            .Should().NotThrowAsync();
    }
    
    /// <summary>
    /// Tester at hvis en bruker prøver å opprette en bruker i en annen avdeling så kastes en
    /// UnauthorizedAccessException-exception
    /// </summary>
    [Fact]
    public async Task Interceptor_CreateUserInForbiddenDepartment_ThrowsError()
    {
        ApplicationUser user = TestDataFactory.CreateApplicationUser(email: "userb@cv.no", 
            departmentId: _departmentB.Id);
        
        await using AppDbContext context = CreateContext(_departmentA.Id);
        context.Users.Add(user);
        
        // Act and assert - Utfører lagring og sjekker at det ikke blir kastet en feil
        await context.Invoking(c => c.SaveChangesAsync())
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }
    
    /// <summary>
    /// Tester at en bruker med bypass får lov til å opprette en bruker i en annen avdeling enn deres egen
    /// </summary>
    [Fact]
    public async Task Interceptor_CreateUserInAnotherDepartmentWithBypass_SavesSuccessfully()
    {
        ApplicationUser user = TestDataFactory.CreateApplicationUser(email: "userb@cv.no", 
            departmentId: _departmentB.Id);
        
        await using AppDbContext context = CreateContext(_departmentA.Id, Permissions.UsersAll);
        context.Users.Add(user);
        
        // Act and assert
        await context.Invoking(c => c.SaveChangesAsync())
            .Should().NotThrowAsync();
    }
    
    /// <summary>
    /// Tester at en bruker med sub-bypass kan opprette en bruker i en underavdeling
    /// </summary>
    [Fact]
    public async Task Interceptor_CreateUserInSubDepartmentWithSubBypass_SavesSuccessfully()
    {
        ApplicationUser user = TestDataFactory.CreateApplicationUser(email: "usersub@cv.no", 
            departmentId: _subDepartment.Id);
        
        await using AppDbContext context = CreateContext(_departmentA.Id, Permissions.UsersReadSub);
        context.Users.Add(user);
        
        // Act and assert
        await context.Invoking(c => c.SaveChangesAsync())
            .Should().NotThrowAsync();
    }
    
    /// <summary>
    /// Tester at oppretting av en bruker i en side-liggende avdeling (ikke en underliggende avdeling)
    /// kaster en exception hvis vi har UsersReadSub (og ikke UsersAll)
    /// </summary>
    [Fact]
    public async Task Interceptor_CreateUserInAnotherDepartmentWithSubBypass_ThrowsException()
    {
        ApplicationUser user = TestDataFactory.CreateApplicationUser(email: "userb@cv.no", 
            departmentId: _departmentB.Id);
        
        await using AppDbContext context = CreateContext(_departmentA.Id, Permissions.UsersReadSub);
        context.Users.Add(user);
        
        // Act and assert
        await context.Invoking(c => c.SaveChangesAsync())
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }
}