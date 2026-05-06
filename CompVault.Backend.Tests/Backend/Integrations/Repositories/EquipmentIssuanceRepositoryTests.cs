using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Equipment;
using CompVault.Backend.Tests.Common;
using CompVault.Shared.Constants;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Backend.Tests.Backend.Integrations.Repositories;

[Collection(nameof(IntegrationTestCollection))]
public class EquipmentIssuanceRepositoryTests(BackendWebApplicationFactory factory) : IAsyncLifetime
{
    private Department _departmentA = null!;
    private Department _departmentB = null!;
    private Department _subDepartment = null!;
    private EquipmentCategory _category = null!;
    private EquipmentItem _item = null!;
    private ApplicationUser _userA = null!;
    private ApplicationUser _userB = null!;
    private ApplicationUser _userSub = null!;
    private ApplicationUser _issuedBy = null!;

    // Seeder avdelinger, brukere, kategor og et utstyr klart for testing
    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();

        _departmentA = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Avdeling A");
        _departmentB = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Avdeling B");
        _subDepartment = await TestDataSeeder.SeedDepartmentAsync(factory.Services, name: "Underavdeling A",
            parentDepartmentId: _departmentA.Id);

        _userA = await TestDataSeeder.SeedUserAsync(factory.Services,
            email: "usera@cv.no", departmentId: _departmentA.Id);
        _userB = await TestDataSeeder.SeedUserAsync(factory.Services,
            email: "userb@cv.no", departmentId: _departmentB.Id);
        _issuedBy = await TestDataSeeder.SeedUserAsync(factory.Services,
            email: "issuer@cv.no", departmentId: _departmentA.Id);
        _userSub = await TestDataSeeder.SeedUserAsync(factory.Services,
            email: "usersub@cv.no", departmentId: _subDepartment.Id);

        _category = await TestDataSeeder.SeedEquipmentCategoryAsync(factory.Services, name: "Uniform");
        _item = await TestDataSeeder.SeedEquipmentItemAsync(factory.Services,
            categoryId: _category.Id, name: "Jakke");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oppretter EquipmentIssuanceRepository med en DbContext og en DepartmentScopeService
    /// </summary>
    private EquipmentIssuanceRepository CreateSut(Guid departmentId, params string[] permissions)
    {
        IServiceScope scope = factory.Services.CreateScope();

        IHttpContextAccessor httpContextAccessor = TestDataSeeder.CreateHttpContextAccessor(departmentId, permissions);
        var departmentScope = new DepartmentScopeService(httpContextAccessor, scope.ServiceProvider);

        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return new EquipmentIssuanceRepository(context, departmentScope);
    }

    /// <summary>
    /// Legger til en utlevering av et utstyr til innsendt DBContext
    /// </summary>
    private async Task AddAndSaveAsync(AppDbContext context, EquipmentIssuance issuance)
    {
        context.EquipmentIssuances.Add(issuance);
        await context.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // GetByIdWithDetailsAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at brukeren henter utlevert utstyr til en bruker samme avdeling
    /// </summary>
    [Fact]
    public async Task GetByIdWithDetailsAsync_IssuanceInOwnDepartment_ReturnsIssuance()
    {
        // Arrange - Oppretter en DbContext og lagrer et utstyr. Bruker A henter utstryet utlevert til
        // en bruker i avdeling A
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        EquipmentIssuance issuance = TestDataFactory.CreateEquipmentIssuance(userId: _userA.Id, itemId: _item.Id,
            issuedById: _issuedBy.Id);

        await AddAndSaveAsync(context, issuance);

        EquipmentIssuanceRepository sut = CreateSut(_departmentA.Id);

        // Act
        EquipmentIssuance? result = await sut.GetByIdWithDetailsAsync(issuance.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(issuance.Id);
    }

    /// <summary>
    /// Tester at brukeren ikke henter utlevert utstyr til en bruker i en annen avdeling
    /// </summary>
    [Fact]
    public async Task GetByIdWithDetailsAsync_IssuanceInAnotherDepartment_ReturnsNull()
    {
        // Arrange - Oppretter en DbContext og lagrer et utstyr. Innlogget bruker er i avdeling B, men utlevering
        // vi prøver å hente er i avdeling A
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        EquipmentIssuance issuance = TestDataFactory.CreateEquipmentIssuance(userId: _userA.Id, itemId: _item.Id,
            issuedById: _issuedBy.Id);

        await AddAndSaveAsync(context, issuance);

        EquipmentIssuanceRepository sut = CreateSut(_departmentB.Id);

        // Act
        EquipmentIssuance? result = await sut.GetByIdWithDetailsAsync(issuance.Id);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tester at bruker kan hente et utlevert utstyr til en annen bruker i en annen avdeling
    /// med EquipmentAll-permission
    /// </summary>
    [Fact]
    public async Task GetByIdWithDetailsAsync_IssuanceInAnotherDepartment_WithBypassPermission_ReturnsIssuance()
    {
        // Arrange - Oppretter en DbContext og lagrer et utstyr. Bruker A henter utstryet utlevert til
        // en bruker i avdeling B, men korrekt permission
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        EquipmentIssuance issuance = TestDataFactory.CreateEquipmentIssuance(userId: _userB.Id, itemId: _item.Id,
            issuedById: _issuedBy.Id);

        await AddAndSaveAsync(context, issuance);

        EquipmentIssuanceRepository sut = CreateSut(_departmentA.Id, Permissions.EquipmentAll);

        // Act
        EquipmentIssuance? result = await sut.GetByIdWithDetailsAsync(issuance.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(issuance.Id);
    }

    /// <summary>
    /// Tester at bruker kan hente utlevert utstyr til en bruker i en underavdeling med ReadSub-tilattelse
    /// </summary>
    [Fact]
    public async Task GetByIdWithDetailsAsync_IssuanceInSubDepartment_WithSubPermission_ReturnsIssuance()
    {
        // Arrange - Oppretter en DbContext og lagrer et utstyr. Bruker A henter utstryet utlevert til
        // en bruker i underavdelingen
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        EquipmentIssuance issuance = TestDataFactory.CreateEquipmentIssuance(userId: _userSub.Id,
            itemId: _item.Id, issuedById: _issuedBy.Id);

        await AddAndSaveAsync(context, issuance);

        EquipmentIssuanceRepository sut = CreateSut(_departmentA.Id, Permissions.EquipmentReadSub);

        // Act
        EquipmentIssuance? result = await sut.GetByIdWithDetailsAsync(issuance.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(issuance.Id);
    }

    /// <summary>
    /// Tester at bruker ikke kan hente utlevert utstyr til en bruker i en underavdeling med ReadSub-tilattelse
    /// </summary>
    [Fact]
    public async Task GetByIdWithDetailsAsync_IssuanceInAnotherDepartment_WithSubPermission_ReturnsNull()
    {
        // Arrange - Oppretter en DbContext og lagrer et utstyr. Bruker A prøver å hente utstyret utlever til
        // en bruker i avdeling B
        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        EquipmentIssuance issuance = TestDataFactory.CreateEquipmentIssuance(userId: _userB.Id,
            itemId: _item.Id, issuedById: _issuedBy.Id);

        await AddAndSaveAsync(context, issuance);

        EquipmentIssuanceRepository sut = CreateSut(_departmentA.Id, Permissions.EquipmentReadSub);

        // Act
        EquipmentIssuance? result = await sut.GetByIdWithDetailsAsync(issuance.Id);

        // Assert
        result.Should().BeNull();
    }
}