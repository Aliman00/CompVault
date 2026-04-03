using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Competencies;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Tests.Common;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Enums;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Backend.Tests.Backend.Integrations.Repositories;

public class CompetencyRepositoryTests(
    BackendWebApplicationFactory factory) : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
{
    private AppDbContext _context = null!;
    private CompetencyRepository _sut = null!;
    private ApplicationUser _testUser = null!;
    private CompetencyType _testType = null!;

    public async Task InitializeAsync()
    {
        await TestDataSeeder.CreateDb(factory.Services);
        _testUser = await TestDataSeeder.SeedUserAsync(factory.Services, id: TestConstants.Users.ActiveUserId);

        IServiceScope scope = factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Opprett en test CompetencyType
        _testType = new CompetencyType
        {
            Name = "Test Type",
            RequiresExpiration = true,
            IsActive = true
        };
        _context.Set<CompetencyType>().Add(_testType);
        await _context.SaveChangesAsync();

        _sut = new CompetencyRepository(_context);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // GetExpiringAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetExpiringAsync returnerer tom liste når ingen competencies utløper.
    /// </summary>
    [Fact]
    public async Task GetExpiringAsync_NoExpiringCompetencies_ReturnsEmptyList()
    {
        // Arrange - Opprett competency med langt i fremtiden expiry
        Competency competency = new()
        {
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(365), // Langt fremme
            Status = CompetencyStatus.Valid,
            IsActive = true
        };
        _context.Set<Competency>().Add(competency);
        await _context.SaveChangesAsync();

        // Act
        IReadOnlyList<Competency> result = await _sut.GetExpiringAsync(null, null);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tester at GetExpiringAsync returnerer competencies som utløper snart.
    /// </summary>
    [Fact]
    public async Task GetExpiringAsync_HasExpiringCompetencies_ReturnsCorrectList()
    {
        // Arrange - Opprett competency med snart utløp
        Competency expiringSoon = new()
        {
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(30), // Innenfor threshold
            Status = CompetencyStatus.ExpiringSoon,
            IsActive = true
        };
        _context.Set<Competency>().Add(expiringSoon);

        Competency valid = new()
        {
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(365), // Utenfor threshold
            Status = CompetencyStatus.Valid,
            IsActive = true
        };
        _context.Set<Competency>().Add(valid);
        await _context.SaveChangesAsync();

        // Act
        IReadOnlyList<Competency> result = await _sut.GetExpiringAsync(null, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(expiringSoon.Id);
    }

    /// <summary>
    /// Tester at GetExpiringAsync returnerer både utløpte og snart utløpte competencies.
    /// Merk: Repository inkluderer BÅDE Expired og ExpiringSoon i resultatet.
    /// </summary>
    [Fact]
    public async Task GetExpiringAsync_ReturnsBothExpiredAndExpiringSoon()
    {
        // Arrange - Fjern eksisterende competencies først for å unngå data fra tidligere tester
        List<Competency> existing = await _context.Set<Competency>().ToListAsync();
        _context.Set<Competency>().RemoveRange(existing);
        await _context.SaveChangesAsync();

        // Opprett allerede utløpt competency
        Competency expired = new()
        {
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-200),
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // Allerede utløpt
            Status = CompetencyStatus.Expired,
            IsActive = true
        };
        _context.Set<Competency>().Add(expired);

        Competency expiringSoon = new()
        {
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            Status = CompetencyStatus.ExpiringSoon,
            IsActive = true
        };
        _context.Set<Competency>().Add(expiringSoon);
        await _context.SaveChangesAsync();

        // Act
        IReadOnlyList<Competency> result = await _sut.GetExpiringAsync(null, null);

        // Assert - GetExpiringAsync returnerer BÅDE Expired og ExpiringSoon
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Id == expired.Id);
        result.Should().Contain(c => c.Id == expiringSoon.Id);
    }

    /// <summary>
    /// Tester at GetExpiringAsync inkluderer competency på grensen (akkurat 90 dager).
    /// </summary>
    [Fact]
    public async Task GetExpiringAsync_ExactlyAtThreshold_Included()
    {
        // Arrange - Fjern eksisterende competencies først
        List<Competency> existing = await _context.Set<Competency>().ToListAsync();
        _context.Set<Competency>().RemoveRange(existing);
        await _context.SaveChangesAsync();

        // Opprett competency på grensen med Valid status
        Competency atThreshold = new()
        {
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddDays(CompetencyStatusCalculator.ExpiringSoonThresholdDays), // Akkurat på grensen
            Status = CompetencyStatus.Valid, // Må oppdateres via UpdateExpiryStatusesAsync
            IsActive = true
        };
        _context.Set<Competency>().Add(atThreshold);
        await _context.SaveChangesAsync();

        // Kall UpdateExpiryStatusesAsync for å oppdatere status til ExpiringSoon
        await _sut.UpdateExpiryStatusesAsync();

        // Act
        IReadOnlyList<Competency> result = await _sut.GetExpiringAsync(null, null);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(atThreshold.Id);
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
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(-1), // Utløpt i går
            Status = CompetencyStatus.Valid, // Feil status
            IsActive = true
        };
        _context.Set<Competency>().Add(expiredCompetency);
        await _context.SaveChangesAsync();

        // Act
        (int expiredCount, int expiringSoonCount) = await _sut.UpdateExpiryStatusesAsync();

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
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(30), // Innenfor threshold
            Status = CompetencyStatus.Valid, // Feil status
            IsActive = true
        };
        _context.Set<Competency>().Add(expiringSoonCompetency);
        await _context.SaveChangesAsync();

        // Act
        (int expiredCount, int expiringSoonCount) = await _sut.UpdateExpiryStatusesAsync();

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
            UserId = _testUser.Id,
            CompetencyTypeId = _testType.Id,
            IssuedDate = DateTime.UtcNow.AddDays(-100),
            ExpiryDate = DateTime.UtcNow.AddDays(365), // Langt fremme
            Status = CompetencyStatus.Valid,
            IsActive = true
        };
        _context.Set<Competency>().Add(validCompetency);
        await _context.SaveChangesAsync();

        // Act
        (int expiredCount, int expiringSoonCount) = await _sut.UpdateExpiryStatusesAsync();

        // Assert
        expiredCount.Should().Be(0);
        expiringSoonCount.Should().Be(0);

        await _context.Entry(validCompetency).ReloadAsync();
        validCompetency.Status.Should().Be(CompetencyStatus.Valid);
    }
}
