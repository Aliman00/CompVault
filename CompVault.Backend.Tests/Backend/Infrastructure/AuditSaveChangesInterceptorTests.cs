using System.Text.Json;

using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Audit.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Data.Interceptors;
using CompVault.Backend.Tests.Common;
using CompVault.Shared.Enums;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Moq;

namespace CompVault.Backend.Tests.Backend.Infrastructure;

public class AuditSaveChangesInterceptorTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IAuditContext> _auditContextMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public AuditSaveChangesInterceptorTests()
    {
        _auditContextMock = new Mock<IAuditContext>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditContext)))
            .Returns(_auditContextMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IHttpContextAccessor)))
            .Returns(_httpContextAccessorMock.Object);

        _auditContextMock.SetupGet(ac => ac.Reason).Returns((string?)null);
        _auditContextMock.SetupGet(ac => ac.ActionOverride).Returns((string?)null);
    }

    private AppDbContext CreateContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditSaveChangesInterceptor(_serviceProviderMock.Object))
            .Options;

        return new AppDbContext(options, new BypassDepartmentScopeService());
    }

    // -------------------------------------------------------------------------
    // Added entry → oppretter AuditLog med action "{entity}.create"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_AddedEntity_CreatesAuditLogWithCreateAction()
    {
        // Arrange
        using AppDbContext context = CreateContext();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "IT",
            Description = "IT-avdelingen",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);

        // Act
        await context.SaveChangesAsync();

        // Assert
        List<AuditLog> auditLogs = await context.AuditLogs.ToListAsync();
        auditLogs.Should().ContainSingle(a => a.Action == "department.create" && a.EntityId == department.Id);
    }

    // -------------------------------------------------------------------------
    // Modified entry → oppretter AuditLog med changed_fields
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_ModifiedEntity_CreatesAuditLogWithChangedFields()
    {
        // Arrange
        using AppDbContext context = CreateContext();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "IT",
            Description = "IT-avdelingen",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);
        await context.SaveChangesAsync();

        // Clear ChangeTracker from the add
        context.ChangeTracker.Clear();

        // Act — modify
        Department? fetched = await context.Departments.FindAsync(department.Id);
        fetched!.Name = "IT og Digitalisering";

        await context.SaveChangesAsync();

        // Assert
        List<AuditLog> auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == department.Id)
            .ToListAsync();

        auditLogs.Should().Contain(a => a.Action == "department.update");
        AuditLog updateLog = auditLogs.First(a => a.Action == "department.update");
        updateLog.Details.Should().NotBeNull();
        Dictionary<string, object>? details = JsonSerializer.Deserialize<Dictionary<string, object>>(updateLog.Details!);
        details.Should().ContainKey("changed_fields");
    }

    // -------------------------------------------------------------------------
    // Soft-delete (DeletedAt: null → verdi) → action "{entity}.delete"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_SoftDelete_CreatesAuditLogWithDeleteAction()
    {
        // Arrange
        using AppDbContext context = CreateContext();

        var competencyType = new CompetencyType
        {
            Id = Guid.NewGuid(),
            Name = "HMS-kurs",
            Category = "HMS",
            RequiresExpiration = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.CompetencyTypes.Add(competencyType);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — soft-delete
        CompetencyType? fetched = await context.CompetencyTypes.FindAsync(competencyType.Id);
        fetched!.DeletedAt = DateTime.UtcNow;
        fetched.IsActive = false;

        await context.SaveChangesAsync();

        // Assert
        List<AuditLog> auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == competencyType.Id)
            .ToListAsync();

        auditLogs.Should().Contain(a => a.Action == "competency_type.delete");
    }

    // -------------------------------------------------------------------------
    // Hard-delete → action "{entity}.delete"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_HardDelete_CreatesAuditLogWithDeleteAction()
    {
        // Arrange
        using AppDbContext context = CreateContext();

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = "TestRole",
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — hard-delete
        ApplicationRole? fetched = await context.Roles.FindAsync(role.Id);
        context.Roles.Remove(fetched!);

        await context.SaveChangesAsync();

        // Assert
        List<AuditLog> auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == role.Id)
            .ToListAsync();

        auditLogs.Should().Contain(a => a.Action == "application_role.delete");
    }

    // -------------------------------------------------------------------------
    // Ignorerte entiteter → ingen AuditLog
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("OtpCode")]
    [InlineData("RefreshToken")]
    [InlineData("AuditLog")]
    [InlineData("DocumentVersion")]
    [InlineData("RolePermission")]
    public async Task SavingChangesAsync_IgnoredEntity_DoesNotCreateAuditLog(string ignoredEntityType)
    {
        // Arrange
        using AppDbContext context = CreateContext();

        // Opprett faktisk entitet av riktig type — InMemory hånddhever ikke FK-constraints
        switch (ignoredEntityType)
        {
            case "OtpCode":
                context.Set<OtpCode>().Add(new OtpCode
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Code = "hash",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow
                });
                break;

            case "RefreshToken":
                context.Set<RefreshToken>().Add(new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Token = "test-token",
                    CreatedAt = DateTime.UtcNow
                });
                break;

            case "AuditLog":
                context.Set<AuditLog>().Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = "test.action",
                    EntityType = "Test",
                    EntityId = Guid.NewGuid(),
                    CreatedAt = DateTime.UtcNow
                });
                break;

            case "DocumentVersion":
                context.Set<DocumentVersion>().Add(new DocumentVersion
                {
                    Id = Guid.NewGuid(),
                    DocumentId = Guid.NewGuid(),
                    Version = 1
                });
                break;

            case "RolePermission":
                context.Set<RolePermission>().Add(new RolePermission
                {
                    RoleId = Guid.NewGuid(),
                    PermissionId = Guid.NewGuid(),
                    GrantedAt = DateTime.UtcNow
                });
                break;
        }

        // Act
        await context.SaveChangesAsync();

        // Assert — ingen AuditLog generert for den ignorerte entitetstypen
        List<AuditLog> auditLogs = await context.AuditLogs
            .Where(a => a.EntityType == ignoredEntityType)
            .ToListAsync();
        auditLogs.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // IAuditContext action override fungerer
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_ActionOverride_ChangesActionOnModifiedEntry()
    {
        // Arrange
        _auditContextMock.SetupGet(ac => ac.ActionOverride).Returns("competency.revoke");
        _auditContextMock.SetupGet(ac => ac.Reason).Returns("Sikkerhetsbrudd");

        using AppDbContext context = CreateContext();

        var competency = new Competency
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompetencyTypeId = Guid.NewGuid(),
            Status = CompetencyStatus.Valid,
            IssuedDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Competencies.Add(competency);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act — modify with action override
        Competency? fetched = await context.Competencies.FindAsync(competency.Id);
        fetched!.Status = CompetencyStatus.Revoked;
        fetched.RevokedReason = "Sikkerhetsbrudd";

        await context.SaveChangesAsync();

        // Assert
        List<AuditLog> auditLogs = await context.AuditLogs
            .Where(a => a.EntityId == competency.Id)
            .ToListAsync();

        auditLogs.Should().Contain(a => a.Action == "competency.revoke");
    }

    // -------------------------------------------------------------------------
    // IAuditContext reason legges i Details
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_WithReason_IncludesReasonInDetails()
    {
        // Arrange
        _auditContextMock.SetupGet(ac => ac.Reason).Returns("Sikkerhetsbrudd ved truckkjøring");

        using AppDbContext context = CreateContext();

        var competency = new Competency
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CompetencyTypeId = Guid.NewGuid(),
            Status = CompetencyStatus.Valid,
            IssuedDate = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Competencies.Add(competency);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        Competency? fetched = await context.Competencies.FindAsync(competency.Id);
        fetched!.Status = CompetencyStatus.Revoked;

        await context.SaveChangesAsync();

        // Assert
        AuditLog? auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == competency.Id && a.Action != "competency.create");

        auditLog.Should().NotBeNull();
        Dictionary<string, JsonElement>? details = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog!.Details!);
        details.Should().ContainKey("reason");
        details!["reason"].GetString().Should().Be("Sikkerhetsbrudd ved truckkjøring");
    }

    // -------------------------------------------------------------------------
    // Ingen HttpContext → UserId null, UserName "System"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_NoHttpContext_UserIdNullUserNameSystem()
    {
        // Arrange — no authenticated user
        _httpContextAccessorMock.SetupGet(h => h.HttpContext).Returns((HttpContext?)null);

        using AppDbContext context = CreateContext();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "HR",
            Description = "HR-avdelingen",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);

        // Act
        await context.SaveChangesAsync();

        // Assert
        AuditLog? auditLog = await context.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == department.Id);

        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().BeNull();
        auditLog.UserName.Should().Be("System");
    }

    // -------------------------------------------------------------------------
    // IAuditContext.Clear() kalles etter SaveChanges
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SavingChangesAsync_ClearsAuditContextAfterSaving()
    {
        // Arrange
        _auditContextMock.SetupGet(ac => ac.Reason).Returns("Test reason");

        using AppDbContext context = CreateContext();

        var department = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Finance",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Departments.Add(department);

        // Act
        await context.SaveChangesAsync();

        // Assert — Clear should have been called
        _auditContextMock.Verify(ac => ac.Clear(), Times.Once);
    }
}