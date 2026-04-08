using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Roles.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Tests.Common;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Roles;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Roles.Services;

public class RoleServiceTests
{
    private readonly Mock<RoleManager<ApplicationRole>> _roleManagerMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<RoleService>> _loggerMock;
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            Mock.Of<IRoleStore<ApplicationRole>>(), null!, null!, null!, null!);
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RoleService>>();

        _sut = new RoleService(
            _roleManagerMock.Object,
            _roleRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "NewRole",
            Description = "Test description"
        };
        var createdById = Guid.NewGuid();

        ApplicationRole? capturedRole = null;
        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(request.Name))
            .ReturnsAsync(false);

        _roleManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .Callback<ApplicationRole>(r => capturedRole = r)
            .ReturnsAsync(IdentityResult.Success);

        _roleRepositoryMock
            .Setup(x => x.GetUserCountsForRolesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        _roleRepositoryMock
            .Setup(x => x.GetPermissionNamesForRoleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        Result<RoleDto> result = await _sut.CreateAsync(request, createdById);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("NewRole");
        result.Value.Description.Should().Be("Test description");
        result.Value.IsSystem.Should().BeFalse();
        capturedRole.Should().NotBeNull();
        capturedRole!.Name.Should().Be("NewRole");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var request = new CreateRoleRequest
        {
            Name = "ExistingRole",
            Description = "Test"
        };

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(request.Name))
            .ReturnsAsync(true);

        // Act
        Result<RoleDto> result = await _sut.CreateAsync(request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("eksisterer allerede");
        _roleManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = "TestRole",
            Description = "Test",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(x => x.GetUserCountsForRolesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { roleId, 5 } });

        _roleRepositoryMock
            .Setup(x => x.GetPermissionNamesForRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { Permissions.RolesRead, Permissions.RolesWrite });

        // Act
        Result<RoleDto> result = await _sut.GetByIdAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(roleId);
        result.Value.Name.Should().Be("TestRole");
        result.Value.UserCount.Should().Be(5);
        result.Value.Permissions.Should().Contain(Permissions.RolesRead);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(nonExistentId.ToString()))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        Result<RoleDto> result = await _sut.GetByIdAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
        result.Error.Message.Should().Contain(nonExistentId.ToString());
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = "OldName",
            Description = "Old description",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        var request = new UpdateRoleRequest
        {
            Name = "UpdatedName",
            Description = "Updated description"
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(request.Name))
            .ReturnsAsync(false);

        _roleManagerMock
            .Setup(x => x.UpdateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleRepositoryMock
            .Setup(x => x.GetUserCountsForRolesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { roleId, 0 } });

        _roleRepositoryMock
            .Setup(x => x.GetPermissionNamesForRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        Result<RoleDto> result = await _sut.UpdateAsync(roleId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("UpdatedName");
        result.Value.Description.Should().Be("Updated description");
        _roleManagerMock.Verify(x => x.UpdateAsync(role), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateRoleRequest { Name = "NewName" };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(nonExistentId.ToString()))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        Result<RoleDto> result = await _sut.UpdateAsync(nonExistentId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
        _roleManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SystemRole_NameChange_ReturnsConflict()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var systemRole = new ApplicationRole
        {
            Id = roleId,
            Name = "Admin",
            IsSystem = true,
            CreatedAt = DateTime.UtcNow
        };

        var request = new UpdateRoleRequest { Name = "NewAdminName" };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(systemRole);

        // Act
        Result<RoleDto> result = await _sut.UpdateAsync(roleId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("systemroller");
        _roleManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new ApplicationRole
        {
            Id = roleId,
            Name = "MyRole",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        var request = new UpdateRoleRequest { Name = "AnotherExistingRole" }; // Different from current name

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(existingRole);

        _roleManagerMock
            .Setup(x => x.RoleExistsAsync(request.Name))
            .ReturnsAsync(true);

        // Act
        Result<RoleDto> result = await _sut.UpdateAsync(roleId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("eksisterer allerede");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = "RoleToDelete",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(x => x.GetUserCountsForRolesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { roleId, 0 } });

        _roleManagerMock
            .Setup(x => x.DeleteAsync(role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        Result<bool> result = await _sut.DeleteAsync(roleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _roleManagerMock.Verify(x => x.DeleteAsync(role), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(nonExistentId.ToString()))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        Result<bool> result = await _sut.DeleteAsync(nonExistentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
        _roleManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_SystemRole_ReturnsConflict()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var systemRole = new ApplicationRole
        {
            Id = roleId,
            Name = "Admin",
            IsSystem = true,
            CreatedAt = DateTime.UtcNow
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(systemRole);

        // Act
        Result<bool> result = await _sut.DeleteAsync(roleId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("systemroller");
        _roleManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RoleWithUsers_ReturnsConflict()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = "RoleWithUsers",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(x => x.GetUserCountsForRolesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { roleId, 3 } });

        // Act
        Result<bool> result = await _sut.DeleteAsync(roleId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("3 brukere");
        _roleManagerMock.Verify(x => x.DeleteAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    #endregion

    #region AssignPermissionsAsync

    [Fact]
    public async Task AssignPermissionsAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var grantedById = Guid.NewGuid();
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = "TestRole",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        var permission1 = new Permission { Id = Guid.NewGuid(), Name = Permissions.RolesRead, Description = "Read roles" };
        var permission2 = new Permission { Id = Guid.NewGuid(), Name = Permissions.RolesWrite, Description = "Write roles" };

        var request = new AssignPermissionsRequest
        {
            PermissionNames = new List<string> { Permissions.RolesRead, Permissions.RolesWrite }
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(x => x.GetPermissionsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { permission1, permission2 });

        _roleRepositoryMock
            .Setup(x => x.GetUserCountsForRolesAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { roleId, 0 } });

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<Result<RoleDto>>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<RoleDto>>> op, CancellationToken _) => op());

        _roleRepositoryMock
            .Setup(x => x.RemoveRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _roleRepositoryMock
            .Setup(x => x.AddRolePermissionsAsync(It.IsAny<IEnumerable<RolePermission>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<RoleDto> result = await _sut.AssignPermissionsAsync(roleId, request, grantedById);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Permissions.Should().Contain(Permissions.RolesRead);
        result.Value.Permissions.Should().Contain(Permissions.RolesWrite);
        _roleRepositoryMock.Verify(x => x.RemoveRolePermissionsAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        _roleRepositoryMock.Verify(x => x.AddRolePermissionsAsync(It.IsAny<IEnumerable<RolePermission>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignPermissionsAsync_WithNonExistentRole_ReturnsNotFound()
    {
        // Arrange
        var nonExistentRoleId = Guid.NewGuid();
        var request = new AssignPermissionsRequest
        {
            PermissionNames = new List<string> { Permissions.RolesRead }
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(nonExistentRoleId.ToString()))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        Result<RoleDto> result = await _sut.AssignPermissionsAsync(nonExistentRoleId, request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
        _roleRepositoryMock.Verify(x => x.GetPermissionsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignPermissionsAsync_SystemRole_ReturnsConflict()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var systemRole = new ApplicationRole
        {
            Id = roleId,
            Name = "Admin",
            IsSystem = true,
            CreatedAt = DateTime.UtcNow
        };

        var request = new AssignPermissionsRequest
        {
            PermissionNames = new List<string> { Permissions.RolesRead }
        };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(systemRole);

        // Act
        Result<RoleDto> result = await _sut.AssignPermissionsAsync(roleId, request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
        result.Error.Message.Should().Contain("systemroller");
    }

    [Fact]
    public async Task AssignPermissionsAsync_WithInvalidPermissionName_ReturnsValidation()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new ApplicationRole
        {
            Id = roleId,
            Name = "TestRole",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };

        var request = new AssignPermissionsRequest
        {
            PermissionNames = new List<string> { "invalid:permission", Permissions.RolesRead }
        };

        var validPermission = new Permission { Id = Guid.NewGuid(), Name = Permissions.RolesRead, Description = "Read roles" };

        _roleManagerMock
            .Setup(x => x.FindByIdAsync(roleId.ToString()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(x => x.GetPermissionsByNamesAsync(It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission> { validPermission }); // Only returns 1 of 2 requested

        // Act
        Result<RoleDto> result = await _sut.AssignPermissionsAsync(roleId, request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Contain("Ugyldige permissions");
        result.Error.Message.Should().Contain("invalid:permission");
    }

    #endregion
}
