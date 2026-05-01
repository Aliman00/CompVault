using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Constants;
using CompVault.Shared.Enums;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace CompVault.Backend.Tests.Backend.Integrations.Repositories;

[Collection(nameof(IntegrationTestCollection))]
public class CompetencyRepositoryTests(BackendWebApplicationFactory factory) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private CompetencyRepository _sut = null!;
    private CompetencyType _testType = null!;
    private Department _departmentA = null!;
    private Department _departmentB = null!;
    private Department _subDepartment = null!;
    private ApplicationUser _userA = null!;
    private ApplicationUser _userB = null!;
    private ApplicationUser _userSub = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();

        _departmentA = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Avdeling A");
        _departmentB = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Avdeling B");
        _subDepartment = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Underavdeling A",
            parentDepartmentId: _departmentA.Id);

        _userA = await TestDataSeeder.SeedUserAsync(factory.Services,
            id: TestConstants.Users.ActiveUserId, departmentId: _departmentA.Id);
        _userB = await TestDataSeeder.SeedUserAsync(factory.Services,
            email: "userb@cv.no", departmentId: _departmentB.Id);
        _userSub = await TestDataSeeder.SeedUserAsync(factory.Services,
            email: "usersub@cv.no", departmentId: _subDepartment.Id);

        _testType = await TestDataSeeder.SeedCompetencyTypeAsync(factory.Services, name: "Test Type",
            requiresExpiration: true);

        IServiceScope scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        IHttpContextAccessor accessor = TestDataSeeder.CreateHttpContextAccessor(_departmentA.Id);
        var departmentScope = new DepartmentScopeService(accessor, scope.ServiceProvider);

        _sut = new CompetencyRepository(_context, departmentScope);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oppretter CompetencyRepository med en DbContext og en DepartmentScopeService
    /// </summary>
    private CompetencyRepository CreateSut(Guid departmentId, params string[] permissions)
    {
        IServiceScope scope = factory.Services.CreateScope();

        IHttpContextAccessor accessor = TestDataSeeder.CreateHttpContextAccessor(departmentId, permissions);
        var departmentScope = new DepartmentScopeService(accessor, scope.ServiceProvider);

        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return new CompetencyRepository(context, departmentScope);
    }

    // -------------------------------------------------------------------------
    // UpdateExpiryStatusesAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UpdateExpiryStatusesAsync oppdaterer utløpte competencies til Expired.
    /// </summary>
    [Fact]
    public async Task UpdateExpiryStatusesAsync_UpdatesExpiredStatus()
    {
        // Arrange - Opprett competency som er utløpt
        Competency expiredCompetency = new()
        {
            UserId = _userA.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // Utløpt i går
            Status = CompetencyStatus.Valid, // Feil status
            IsActive = true
        };
        _context.Set<Competency>().Add(expiredCompetency);
        await _context.SaveChangesAsync();

        // Act
        (int expiredCount, int expiringSoonCount, _) = await _sut.UpdateExpiryStatusesAsync();

        // Assert
        expiredCount.Should().Be(1);
        expiringSoonCount.Should().Be(0);

        await _context.Entry(expiredCompetency).ReloadAsync();
        expiredCompetency.Status.Should().Be(CompetencyStatus.Expired);
    }

    /// <summary>
    /// Tester at UpdateExpiryStatusesAsync oppdaterer snart utløpte competencies til ExpiringSoon.
    /// </summary>
    [Fact]
    public async Task UpdateExpiryStatusesAsync_UpdatesExpiringSoonStatus()
    {
        // Arrange - Opprett competency som utløper snart
        Competency expiringSoonCompetency = new()
        {
            UserId = _userA.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(30), // Innenfor threshold
            Status = CompetencyStatus.Valid, // Feil status
            IsActive = true
        };
        _context.Set<Competency>().Add(expiringSoonCompetency);
        await _context.SaveChangesAsync();

        // Act
        (int expiredCount, int expiringSoonCount, _) = await _sut.UpdateExpiryStatusesAsync();

        // Assert
        expiredCount.Should().Be(0);
        expiringSoonCount.Should().Be(1);

        await _context.Entry(expiringSoonCompetency).ReloadAsync();
        expiringSoonCompetency.Status.Should().Be(CompetencyStatus.ExpiringSoon);
    }

    /// <summary>
    /// Tester at UpdateExpiryStatusesAsync lar gyldige competencies være Valid.
    /// </summary>
    [Fact]
    public async Task UpdateExpiryStatusesAsync_UpdatesValidStatus()
    {
        // Arrange - Opprett competency med langt fremme expiry
        Competency validCompetency = new()
        {
            UserId = _userA.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(365), // Langt fremme
            Status = CompetencyStatus.Valid,
            IsActive = true
        };
        _context.Set<Competency>().Add(validCompetency);
        await _context.SaveChangesAsync();

        // Act
        (int expiredCount, int expiringSoonCount, _) = await _sut.UpdateExpiryStatusesAsync();

        // Assert
        expiredCount.Should().Be(0);
        expiringSoonCount.Should().Be(0);

        await _context.Entry(validCompetency).ReloadAsync();
        validCompetency.Status.Should().Be(CompetencyStatus.Valid);
    }

    // -------------------------------------------------------------------------
    // ApplyDepartmentFilter Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at brukeren i avdeling A ikke får sett kompetansebeviset til brukeren i avdeling B uten tilattelse
    /// </summary>
    [Fact]
    public async Task GetAllWithDetailsAsync_UserInDepartmentA_DoesNotSeeCompetenciesFromDepartmentB()
    {
        // Arrange
        using IServiceScope seedScope = factory.Services.CreateScope();
        AppDbContext seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Competency competencyA = TestDataFactory.CreateCompetency(
            userId: _userA.Id, competencyTypeId: _testType.Id);
        Competency competencyB = TestDataFactory.CreateCompetency(
            userId: _userB.Id, competencyTypeId: _testType.Id);

        seedContext.Competencies.AddRange(competencyA, competencyB);
        await seedContext.SaveChangesAsync();

        CompetencyRepository sut = CreateSut(_departmentA.Id);

        // Act
        IReadOnlyList<Competency> result = await sut.GetAllWithDetailsAsync(null, null,
            null);

        // Assert
        result.Should().Contain(c => c.Id == competencyA.Id);
        result.Should().NotContain(c => c.Id == competencyB.Id);
    }

    /// <summary>
    /// Tester at brukeren i avdeling A får sett kompetansebeviset til brukeren i avdeling B med
    /// CompetenciesAll-tilattelse
    /// </summary>
    [Fact]
    public async Task GetAllWithDetailsAsync_UserInDepartmentA_SeesCompetenciesFromDepartmentB()
    {
        // Arrange
        using IServiceScope seedScope = factory.Services.CreateScope();
        AppDbContext seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Competency competencyA = TestDataFactory.CreateCompetency(
            userId: _userA.Id, competencyTypeId: _testType.Id);
        Competency competencyB = TestDataFactory.CreateCompetency(
            userId: _userB.Id, competencyTypeId: _testType.Id);

        seedContext.Competencies.AddRange(competencyA, competencyB);
        await seedContext.SaveChangesAsync();

        CompetencyRepository sut = CreateSut(_departmentA.Id, Permissions.CompetenciesAll);

        // Act
        IReadOnlyList<Competency> result = await sut.GetAllWithDetailsAsync(null, null,
            null);

        // Assert
        result.Should().Contain(c => c.Id == competencyA.Id);
        result.Should().Contain(c => c.Id == competencyB.Id);
    }

    /// <summary>
    /// Tester at brukeren i avdeling A ikke får sett kompetansebeviset til brukeren i underavdeling A uten tilattelse
    /// </summary>
    [Fact]
    public async Task GetAllWithDetailsAsync_UserInDepartmentA_DoesNotSeeCompetenciesFromSubDepartment()
    {
        // Arrange
        using IServiceScope seedScope = factory.Services.CreateScope();
        AppDbContext seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Competency competencyA = TestDataFactory.CreateCompetency(
            userId: _userA.Id, competencyTypeId: _testType.Id);
        Competency competencySub = TestDataFactory.CreateCompetency(
            userId: _userSub.Id, competencyTypeId: _testType.Id);

        seedContext.Competencies.AddRange(competencyA, competencySub);
        await seedContext.SaveChangesAsync();

        CompetencyRepository sut = CreateSut(_departmentA.Id);

        // Act
        IReadOnlyList<Competency> result = await sut.GetAllWithDetailsAsync(null, null,
            null);

        // Assert
        result.Should().Contain(c => c.Id == competencyA.Id);
        result.Should().NotContain(c => c.Id == competencySub.Id);
    }

    /// <summary>
    /// Tester at brukeren i avdeling A får sett kompetansebeviset til brukeren i underavdeling A med
    /// CompetenciesReadSub-tilattelse
    /// </summary>
    [Fact]
    public async Task GetAllWithDetailsAsync_UserInDepartmentA_SeesCompetenciesFromSubDepartment()
    {
        // Arrange
        using IServiceScope seedScope = factory.Services.CreateScope();
        AppDbContext seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Competency competencyA = TestDataFactory.CreateCompetency(
            userId: _userA.Id, competencyTypeId: _testType.Id);
        Competency competencySub = TestDataFactory.CreateCompetency(
            userId: _userSub.Id, competencyTypeId: _testType.Id);

        seedContext.Competencies.AddRange(competencyA, competencySub);
        await seedContext.SaveChangesAsync();

        CompetencyRepository sut = CreateSut(_departmentA.Id, Permissions.CompetenciesReadSub);

        // Act
        IReadOnlyList<Competency> result = await sut.GetAllWithDetailsAsync(null, null,
            null);

        // Assert
        result.Should().Contain(c => c.Id == competencyA.Id);
        result.Should().Contain(c => c.Id == competencySub.Id);
    }
}