using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Tests.Common;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
namespace CompVault.Backend.Tests.Backend.Integrations.Repositories;

[Collection(nameof(IntegrationTestCollection))]
public class DepartmentRepositoryTests(
    BackendWebApplicationFactory factory) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private DepartmentRepository _sut = null!;
    private IServiceScope _scope = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();

        _scope = factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _sut = new DepartmentRepository(_context);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HasSubDepartmentsAsync_WithSubDepartments_ReturnsTrue()
    {
        // Arrange
        Department parent = TestDataFactory.CreateDepartment(name: "Parent");
        Department child = TestDataFactory.CreateDepartment(name: "Child", parentDepartmentId: parent.Id);

        _context.Set<Department>().AddRange(parent, child);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _sut.HasSubDepartmentsAsync(parent.Id);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        _context.Set<Department>().RemoveRange(parent, child);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task HasSubDepartmentsAsync_WithoutSubDepartments_ReturnsFalse()
    {
        // Arrange
        Department department = TestDataFactory.CreateDepartment(name: "Lonely Department");

        _context.Set<Department>().Add(department);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _sut.HasSubDepartmentsAsync(department.Id);

        // Assert
        result.Should().BeFalse();

        // Cleanup
        _context.Set<Department>().Remove(department);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task HasMembersAsync_WithMembers_ReturnsTrue()
    {
        // Arrange - Create department with a user assigned to it
        Department department = TestDataFactory.CreateDepartment(name: "HR Department");
        _context.Set<Department>().Add(department);

        ApplicationUser user = TestDataFactory.CreateApplicationUser(email: "hr.user@compvault.test");
        user.DepartmentId = department.Id;
        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        // Act
        bool result = await _sut.HasMembersAsync(department.Id);

        // Assert
        result.Should().BeTrue();

        // Cleanup
        _context.Users.Remove(user);
        _context.Set<Department>().Remove(department);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task HasMembersAsync_WithoutMembers_ReturnsFalse()
    {
        // Arrange
        Department department = TestDataFactory.CreateDepartment(name: "Empty Department");

        _context.Set<Department>().Add(department);
        await _context.SaveChangesAsync();

        // Act
        bool result = await _sut.HasMembersAsync(department.Id);

        // Assert
        result.Should().BeFalse();

        // Cleanup
        _context.Set<Department>().Remove(department);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAncestorIdsAsync_WithAncestors_ReturnsCorrectOrder()
    {
        // Arrange - A -> B -> C (A is grandparent of C)
        Department deptA = TestDataFactory.CreateDepartment(name: "A");
        Department deptB = TestDataFactory.CreateDepartment(name: "B", parentDepartmentId: deptA.Id);
        Department deptC = TestDataFactory.CreateDepartment(name: "C", parentDepartmentId: deptB.Id);

        _context.Set<Department>().AddRange(deptA, deptB, deptC);
        await _context.SaveChangesAsync();

        // Act
        IReadOnlyList<Guid> result = await _sut.GetAncestorIdsAsync(deptC.Id);

        // Assert
        result.Should().HaveCount(2);
        result[0].Should().Be(deptB.Id); // Direct parent
        result[1].Should().Be(deptA.Id); // Grandparent

        // Cleanup - clear navigation properties first to avoid EF Core issues
        deptC.ParentDepartmentId = null;
        deptB.ParentDepartmentId = null;
        _context.Set<Department>().RemoveRange(deptA, deptB, deptC);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAncestorIdsAsync_WithNoParent_ReturnsEmptyList()
    {
        // Arrange - Root department (no parent)
        Department root = TestDataFactory.CreateDepartment(name: "Root Department");

        _context.Set<Department>().Add(root);
        await _context.SaveChangesAsync();

        // Act
        IReadOnlyList<Guid> result = await _sut.GetAncestorIdsAsync(root.Id);

        // Assert
        result.Should().BeEmpty();

        // Cleanup
        _context.Set<Department>().Remove(root);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAncestorIdsAsync_WithDeepHierarchy_ReturnsAllAncestors()
    {
        // Arrange - A -> B -> C -> D -> E (5 levels deep)
        Department deptA = TestDataFactory.CreateDepartment(name: "Level 0");
        Department deptB = TestDataFactory.CreateDepartment(name: "Level 1", parentDepartmentId: deptA.Id);
        Department deptC = TestDataFactory.CreateDepartment(name: "Level 2", parentDepartmentId: deptB.Id);
        Department deptD = TestDataFactory.CreateDepartment(name: "Level 3", parentDepartmentId: deptC.Id);
        Department deptE = TestDataFactory.CreateDepartment(name: "Level 4", parentDepartmentId: deptD.Id);

        _context.Set<Department>().AddRange(deptA, deptB, deptC, deptD, deptE);
        await _context.SaveChangesAsync();

        // Act - Get ancestors of E
        IReadOnlyList<Guid> result = await _sut.GetAncestorIdsAsync(deptE.Id);

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().Be(deptD.Id);
        result[1].Should().Be(deptC.Id);
        result[2].Should().Be(deptB.Id);
        result[3].Should().Be(deptA.Id);

        // Cleanup - clear navigation properties first
        deptE.ParentDepartmentId = null;
        deptD.ParentDepartmentId = null;
        deptC.ParentDepartmentId = null;
        deptB.ParentDepartmentId = null;
        _context.Set<Department>().RemoveRange(deptA, deptB, deptC, deptD, deptE);
        await _context.SaveChangesAsync();
    }
}