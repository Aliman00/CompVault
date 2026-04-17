using System.Linq.Expressions;

using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Tests.Common;
using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

using FluentAssertions;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Departments.Services;

public class DepartmentServiceTests
{
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly DepartmentService _sut;

    public DepartmentServiceTests()
    {
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _sut = new DepartmentService(_departmentRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDepartmentsWithSubDepartmentCount()
    {
        // Arrange
        Department departmentA = TestDataFactory.CreateDepartment(name: "Avdeling A");
        Department departmentB = TestDataFactory.CreateDepartment(name: "Avdeling B");

        _departmentRepositoryMock
            .Setup(x => x.GetAllWithHierarchyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { departmentA, departmentB });

        // Act
        Result<IReadOnlyList<DepartmentDto>> result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _departmentRepositoryMock.Verify(x => x.GetAllWithHierarchyAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsDepartment()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(id: departmentId, name: "HR");

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        Result<DepartmentDto> result = await _sut.GetByIdAsync(departmentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("HR");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        Result<DepartmentDto> result = await _sut.GetByIdAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsDepartment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateDepartmentRequest
        {
            Name = "New Department",
            Description = "Test description"
        };

        Department? capturedDepartment = null;
        _departmentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()))
            .Callback<Department, CancellationToken>((d, _) => capturedDepartment = d);

        // Act
        Result<DepartmentDto> result = await _sut.CreateAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Department");
        capturedDepartment.Should().NotBeNull();
        capturedDepartment!.Name.Should().Be("New Department");
        capturedDepartment.IsActive.Should().BeTrue();
        capturedDepartment!.CreatedById.Should().Be(userId);

        _departmentRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentOrInactiveParent_ReturnsNotFound()
    {
        // Arrange - Parent does not exist OR is inactive - both return false from ExistsAsync
        var userId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var request = new CreateDepartmentRequest
        {
            Name = "Child Department",
            ParentDepartmentId = parentId
        };

        _departmentRepositoryMock
            .Setup(x => x.ExistsAsync(
                It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<DepartmentDto> result = await _sut.CreateAsync(userId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
        _departmentRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Department>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsUpdatedDepartment()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(id: departmentId, name: "Old Name");

        var request = new UpdateDepartmentRequest
        {
            Name = "Updated Name",
            Description = "Updated description"
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        Result<DepartmentDto> result = await _sut.UpdateAsync(departmentId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Updated Name");
        _departmentRepositoryMock.Verify(x => x.UpdateAsync(department, It.IsAny<CancellationToken>()), Times.Once);
        _departmentRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateDepartmentRequest { Name = "New Name" };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        Result<DepartmentDto> result = await _sut.UpdateAsync(nonExistentId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_WithSelfAsParent_ReturnsUnprocessableEntity()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(id: departmentId, name: "Self Ref");

        var request = new UpdateDepartmentRequest
        {
            ParentDepartmentId = departmentId
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        Result<DepartmentDto> result = await _sut.UpdateAsync(departmentId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Contain("ikke være sin egen forelder");
    }

    [Fact]
    public async Task UpdateAsync_WithDescendantAsParent_ReturnsUnprocessableEntity()
    {
        // Arrange - A -> C (A is parent of C), trying to set C as parent of A (circular: A -> C -> A)
        var departmentAId = Guid.NewGuid();
        var departmentCId = Guid.NewGuid();

        Department departmentA = TestDataFactory.CreateDepartment(id: departmentAId, name: "A");
        Department departmentC = TestDataFactory.CreateDepartment(id: departmentCId, name: "C", parentDepartmentId: departmentAId);

        var request = new UpdateDepartmentRequest
        {
            ParentDepartmentId = departmentCId
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(departmentAId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(departmentA);

        _departmentRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _departmentRepositoryMock
            .Setup(x => x.GetAncestorIdsAsync(departmentCId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { departmentAId }); // C's ancestor is A

        // Act
        Result<DepartmentDto> result = await _sut.UpdateAsync(departmentAId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Contain("underavdeling til å være forelder");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentOrInactiveParent_ReturnsNotFound()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(id: departmentId, name: "Test");

        var request = new UpdateDepartmentRequest
        {
            ParentDepartmentId = parentId
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _departmentRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<DepartmentDto> result = await _sut.UpdateAsync(departmentId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ClearingParent_SetsParentToNull()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(
            id: departmentId,
            name: "Test",
            parentDepartmentId: parentId);

        var request = new UpdateDepartmentRequest
        {
            ClearParentDepartment = true
        };

        _departmentRepositoryMock
            .Setup(x => x.GetByIdWithHierarchyAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        Result<DepartmentDto> result = await _sut.UpdateAsync(departmentId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        department.ParentDepartmentId.Should().BeNull();
        _departmentRepositoryMock.Verify(x => x.UpdateAsync(department, It.IsAny<CancellationToken>()), Times.Once);
        _departmentRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(id: departmentId, name: "To Delete");

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _departmentRepositoryMock
            .Setup(x => x.HasSubDepartmentsAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _departmentRepositoryMock
            .Setup(x => x.HasMembersAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<bool> result = await _sut.DeleteAsync(departmentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _departmentRepositoryMock.Verify(x => x.SoftDeleteAsync(department, It.IsAny<CancellationToken>()), Times.Once);
        _departmentRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Department?)null);

        // Act
        Result<bool> result = await _sut.DeleteAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_WithSubDepartments_ReturnsConflict()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(id: departmentId, name: "Parent");

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _departmentRepositoryMock
            .Setup(x => x.HasSubDepartmentsAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<bool> result = await _sut.DeleteAsync(departmentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("underavdelinger");
    }

    [Fact]
    public async Task DeleteAsync_WithMembers_ReturnsConflict()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        Department department = TestDataFactory.CreateDepartment(id: departmentId, name: "With Members");

        _departmentRepositoryMock
            .Setup(x => x.GetByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        _departmentRepositoryMock
            .Setup(x => x.HasSubDepartmentsAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _departmentRepositoryMock
            .Setup(x => x.HasMembersAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<bool> result = await _sut.DeleteAsync(departmentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("medlemmer");
    }
}