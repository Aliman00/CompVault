using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Enums;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
namespace CompVault.Backend.Tests.Backend.Integrations.Repositories;

[Collection(nameof(IntegrationTestCollection))]
public class CompetencyTypeRepositoryTests(
    BackendWebApplicationFactory factory) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private CompetencyTypeRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await TestDataSeeder.SeedUserAsync(factory.Services, id: TestConstants.Users.ActiveUserId);

        IServiceScope scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _sut = new CompetencyTypeRepository(_context);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GetByNameAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetByNameAsync finner type med eksakt navn.
    /// </summary>
    [Fact]
    public async Task GetByNameAsync_ExactMatch_ReturnsType()
    {
        // Arrange
        CompetencyType type = new()
        {
            Name = "Førerkort B",
            IsActive = true
        };
        _context.Set<CompetencyType>().Add(type);
        await _context.SaveChangesAsync();

        // Act
        CompetencyType? result = await _sut.GetByNameAsync("Førerkort B");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(type.Id);
    }

    /// <summary>
    /// Tester at GetByNameAsync er case-insensitive.
    /// </summary>
    [Fact]
    public async Task GetByNameAsync_DifferentCase_ReturnsType()
    {
        // Arrange
        CompetencyType type = new()
        {
            Name = "Førerkort B",
            IsActive = true
        };
        _context.Set<CompetencyType>().Add(type);
        await _context.SaveChangesAsync();

        // Act
        CompetencyType? result = await _sut.GetByNameAsync("FØRERKORT B");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(type.Id);
    }

    /// <summary>
    /// Tester at GetByNameAsync returnerer null når type ikke finnes.
    /// </summary>
    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        // Act
        CompetencyType? result = await _sut.GetByNameAsync("Ikke eksisterende");

        // Assert
        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // HasCompetenciesAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at HasCompetenciesAsync returnerer true når det finnes aktive competencies.
    /// </summary>
    [Fact]
    public async Task HasCompetenciesAsync_ActiveCompetencies_ReturnsTrue()
    {
        // Arrange
        CompetencyType type = new()
        {
            Name = "Test Type",
            IsActive = true
        };
        _context.Set<CompetencyType>().Add(type);

        Competency competency = new()
        {
            CompetencyTypeId = type.Id,
            UserId = TestConstants.Users.ActiveUserId,
            IssuedDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddDays(100),
            Status = CompetencyStatus.Valid, // Aktiv status
            IsActive = true
        };
        _context.Set<Competency>().Add(competency);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _sut.HasCompetenciesAsync(type.Id);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tester at HasCompetenciesAsync ignorerer bare archived/expired/revoked competencies.
    /// </summary>
    [Fact]
    public async Task HasCompetenciesAsync_OnlyArchived_ReturnsFalse()
    {
        // Arrange
        CompetencyType type = new()
        {
            Name = "Test Type",
            IsActive = true
        };
        _context.Set<CompetencyType>().Add(type);

        // Kun expired competency
        Competency competency = new()
        {
            CompetencyTypeId = type.Id,
            UserId = TestConstants.Users.ActiveUserId,
            IssuedDate = DateTime.UtcNow.AddDays(-200),
            ExpiryDate = DateTime.UtcNow.AddDays(-100),
            Status = CompetencyStatus.Expired, // Ikke aktiv
            IsActive = true
        };
        _context.Set<Competency>().Add(competency);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _sut.HasCompetenciesAsync(type.Id);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tester at HasCompetenciesAsync returnerer false når det ikke finnes competencies.
    /// </summary>
    [Fact]
    public async Task HasCompetenciesAsync_NoCompetencies_ReturnsFalse()
    {
        // Arrange
        CompetencyType type = new()
        {
            Name = "Test Type",
            IsActive = true
        };
        _context.Set<CompetencyType>().Add(type);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _sut.HasCompetenciesAsync(type.Id);

        // Assert
        result.Should().BeFalse();
    }
}