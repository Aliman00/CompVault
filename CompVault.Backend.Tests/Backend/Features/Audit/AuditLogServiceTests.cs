using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Backend.Features.Audit.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Tests.Backend.Features.Audit;

/// <summary>
/// Tester AuditLogService med ekte InMemory-database.
/// </summary>
public class AuditLogServiceTests : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private AuditLogService _sut = null!;

    public async Task InitializeAsync()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new AuditLogService(_context);

        // Seed testdata
        await SeedAuditDataAsync();
        await _context.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Hent alle uten filtrering
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_NoFilters_ReturnsAllEntries()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
    }

    // -------------------------------------------------------------------------
    // Filtrering på action
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_FilterByAction_ReturnsMatchingEntries()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters { Action = "competency.revoke" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(a => a.Action == "competency.revoke");
        result.Value.TotalCount.Should().Be(1);
    }

    // -------------------------------------------------------------------------
    // Filtrering på entityType
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_FilterByEntityType_ReturnsMatchingEntries()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters { EntityType = "Department" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(a => a.EntityType == "Department");
        result.Value.TotalCount.Should().Be(2);
    }

    // -------------------------------------------------------------------------
    // Filtrering på userId
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_FilterByUserId_ReturnsMatchingEntries()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters { UserId = TestUserId });

        // Assert — TestUserId appears in competency.revoke, competency.create, and department.create
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(a => a.UserId == TestUserId);
        result.Value.TotalCount.Should().Be(3);
    }

    // -------------------------------------------------------------------------
    // Filtrering på entityId
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_FilterByEntityId_ReturnsMatchingEntries()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters { EntityId = _departmentId });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(a => a.EntityId == _departmentId);
    }

    // -------------------------------------------------------------------------
    // Paginering
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_Pagination_PageSizeLimitsResults()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters { Page = 1, PageSize = 2 });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task GetAsync_Pagination_SecondPage()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters { Page = 2, PageSize = 2 });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(2);
    }

    // -------------------------------------------------------------------------
    // PageSize max 100
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_PageSizeOver100_CapsAt100()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters { PageSize = 200 });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(100);
    }

    // -------------------------------------------------------------------------
    // Sortering — nyeste først
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_DefaultSort_NewestFirst()
    {
        // Act
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters());

        // Assert
        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items.ToList();
        for (int i = 1; i < items.Count; i++)
        {
            items[i].CreatedAt.Should().BeOnOrBefore(items[i - 1].CreatedAt);
        }
    }

    // -------------------------------------------------------------------------
    // Filtrering på dato
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_FilterByDate_ReturnsEntriesInRange()
    {
        // Act — bruker bredt intervall for å fange opp alle seedede entries
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters
        {
            From = DateTime.UtcNow.AddDays(-10),
            To = DateTime.UtcNow.AddDays(10)
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(5);
    }

    // -------------------------------------------------------------------------
    // Filtrering på dato — ingen treff
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_FilterByDate_NoMatches_ReturnsEmpty()
    {
        // Act — intervall før alle entries
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters
        {
            From = DateTime.UtcNow.AddDays(-100),
            To = DateTime.UtcNow.AddDays(-50)
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Kombinert filtrering
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_CombinedFilters_ReturnsMatchingEntries()
    {
        // Act — department=create utført av TestUserId
        Result<PagedResult<AuditLogDto>> result = await _sut.GetAsync(new AuditLogQueryParameters
        {
            EntityType = "Department",
            UserId = TestUserId
        });

        // Assert — TestUserId har department.create og department.update, men kun department.create med matching entityType+userId
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(a => a.Action == "department.create");
    }

    // -------------------------------------------------------------------------
    // Hjelpemetoder og testdata
    // -------------------------------------------------------------------------

    private static readonly Guid TestUserId = Guid.NewGuid();
    private Guid _departmentId;

    private async Task SeedAuditDataAsync()
    {
        _departmentId = Guid.NewGuid();

        _context.AuditLogs.AddRange(
            new AuditLog
            {
                Action = "competency.revoke",
                EntityType = "Competency",
                EntityId = Guid.NewGuid(),
                UserId = TestUserId,
                UserName = "Test User",
                UserEmail = "test@example.com",
                Details = """{"reason":"Sikkerhetsbrudd"}""",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new AuditLog
            {
                Action = "competency.create",
                EntityType = "Competency",
                EntityId = Guid.NewGuid(),
                UserId = TestUserId,
                UserName = "Test User",
                UserEmail = "test@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new AuditLog
            {
                Action = "department.create",
                EntityType = "Department",
                EntityId = _departmentId,
                UserId = TestUserId,
                UserName = "Test User",
                UserEmail = "test@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new AuditLog
            {
                Action = "department.update",
                EntityType = "Department",
                EntityId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                UserName = "Admin User",
                UserEmail = "admin@example.com",
                Details = """{"changed_fields":{"Name":{"old":"IT","new":"IT og Digitalisering"}}}""",
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            },
            new AuditLog
            {
                Action = "competency.status_auto_update",
                EntityType = "Competency",
                EntityId = Guid.NewGuid(),
                UserId = null,
                UserName = "System",
                Details = """{"old_status":"Valid","new_status":"Expired"}""",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}