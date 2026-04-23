using System.Linq.Expressions;

using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Features.Users.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Moq;
namespace CompVault.Backend.Tests.Backend.Features.Users;

public class UserServiceTests
{
    // Mocker avhengighetene UserService trenger
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IJobTitleRepository> _jobTitleRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    // Systemet vi tester
    private readonly UserService _sut;

    // Testbruker som brukes på tvers av testene
    private readonly ApplicationUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        UserName = "test@example.com",
        FirstName = "Ola",
        LastName = "Nordmann",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _jobTitleRepositoryMock = new Mock<IJobTitleRepository>();

        // UserManager krever en IUserStore-mock for å kunne instansieres
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        // UserManager har ingen tom konstruktør — IUserStore er eneste påkrevde parameter.
        // Resten settes til null! siden de ikke brukes i disse testene.
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // RoleManager krever en IRoleStore-mock for å kunne instansieres
        var roleStoreMock = new Mock<IRoleStore<ApplicationRole>>();
        var roleManagerMock = new Mock<RoleManager<ApplicationRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        // Logger mock
        var loggerMock = new Mock<ILogger<UserService>>();

        var unitOfWorkMock = new Mock<IUnitOfWork>();

        unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<Task<Result<UserDto>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<UserDto>>> op, CancellationToken _) => op());

        _sut = new UserService(
            _userRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _jobTitleRepositoryMock.Object,
            _userManagerMock.Object,
            roleManagerMock.Object,
            loggerMock.Object,
            unitOfWorkMock.Object);
    }

    /// <summary>
    /// Tester at GetUserByIdAsync returnerer brukeren når den eksisterer og er aktiv
    /// </summary>
    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(new List<string> { "Employee" });

        // Act
        Result<UserDto> result = await _sut.GetUserByIdAsync(_testUser.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(_testUser.Email, result.Value!.Email);
    }

    /// <summary>
    /// Tester at GetUserByIdAsync returnerer NotFound når brukeren ikke eksisterer
    /// </summary>
    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<UserDto> result = await _sut.GetUserByIdAsync(Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error!.Code);
    }

    /// <summary>
    /// Tester at CreateUserAsync oppretter en bruker og returnerer riktig DTO når alt går bra
    /// </summary>
    [Fact]
    public async Task CreateUserAsync_WhenEmailIsAvailable_ReturnsCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "ny@example.com",
            FirstName = "Ny",
            LastName = "Bruker",
            Roles = []
        };

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // E-post ikke tatt

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        // Act
        Result<UserDto> result = await _sut.CreateUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Email.ToLowerInvariant(), result.Value!.Email);
        Assert.Equal(request.FirstName, result.Value.FirstName);
    }


    /// <summary>
    /// Tester at CreateUserAsync returnerer Conflict når e-posten allerede er i bruk
    /// </summary>
    [Fact]
    public async Task CreateUserAsync_WhenEmailAlreadyExists_ReturnsConflict()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = _testUser.Email!,
            FirstName = "Test",
            LastName = "Bruker",
            Roles = []
        };

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<UserDto> result = await _sut.CreateUserAsync(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Conflict, result.Error!.Code);
    }

    /// <summary>
    /// Tester at DeleteUserAsync setter brukeren som soft-deleted og returnerer success
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WhenUserExists_CallsSoftDeleteAndReturnsSuccess()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userRepositoryMock
            .Setup(r => r.SoftDeleteAsync(_testUser, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<bool> result = await _sut.DeleteUserAsync(_testUser.Id);

        // Assert - Sjekker at SoftDelete faktisk ble kalt og at resultatet er success
        Assert.True(result.IsSuccess);
        _userRepositoryMock.Verify(r => r.SoftDeleteAsync(_testUser, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tester at DeleteUserAsync returnerer NotFound når brukeren ikke eksisterer
    /// </summary>
    [Fact]
    public async Task DeleteUserAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<bool> result = await _sut.DeleteUserAsync(Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error!.Code);
    }

    // -------------------------------------------------------------------------
    // GetAllUsersAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetAllUsersAsync returnerer en liste med mappede UserDto-er for alle aktive brukere
    /// </summary>
    [Fact]
    public async Task GetAllUsersAsync_ReturnsListOfActiveUsers()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "bruker1@example.com",
            UserName = "bruker1@example.com",
            FirstName = "Ola",
            LastName = "Nordmann",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "bruker2@example.com",
            UserName = "bruker2@example.com",
            FirstName = "Kari",
            LastName = "Nordmann",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var usersWithRoles = new List<(ApplicationUser User, List<string> Roles)>
        {
            (user1, ["Employee"]),
            (user2, ["Admin"])
        };

        _userRepositoryMock
            .Setup(r => r.CountActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _userRepositoryMock
            .Setup(r => r.GetActiveUsersWithRolesPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersWithRoles);

        // Act
        Result<PagedResult<UserDto>> result = await _sut.GetAllUsersAsync(new PagedQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().Contain(u => u.Email == user1.Email);
        result.Value!.Items.Should().Contain(u => u.Email == user2.Email);
    }

    /// <summary>
    /// Tester at GetAllUsersAsync returnerer en tom liste når det ikke finnes aktive brukere
    /// </summary>
    [Fact]
    public async Task GetAllUsersAsync_WhenNoUsers_ReturnsEmptyList()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.CountActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _userRepositoryMock
            .Setup(r => r.GetActiveUsersWithRolesPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<(ApplicationUser User, List<string> Roles)>());

        // Act
        Result<PagedResult<UserDto>> result = await _sut.GetAllUsersAsync(new PagedQuery());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // UpdateUserAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UpdateUserAsync oppdaterer felt og returnerer oppdatert UserDto
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenUserExists_UpdatesFieldsAndReturnsDto()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "Nytt",
            LastName = "Navn",
            JobTitleId = Guid.NewGuid(),
            Email = "ny@example.com"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _jobTitleRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<JobTitle, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(_testUser, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(new List<string> { "Employee" });

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Nytt");
        result.Value!.LastName.Should().Be("Navn");
        result.Value!.JobTitleId.Should().NotBeNull();
        result.Value!.Email.Should().Be("ny@example.com");

        _userRepositoryMock.Verify(r => r.UpdateAsync(_testUser, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tester at roller oppdateres for brukeren og returner korrekt Dto
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenRolesProvided_AssignsRolesAndReturnsDto()
    {
        // Arrange - Setter at brukeren har Employee fra før, men byttes til Admin
        var request = new UpdateUserRequest { Roles = ["Admin"] };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(["Employee"]);

        _userManagerMock
            .Setup(m => m.RemoveFromRolesAsync(_testUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRolesAsync(_testUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(_testUser, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert - Sjekker at vi har prøvd å fjerne og legge til rollene
        result.IsSuccess.Should().BeTrue();
        _userManagerMock.Verify(m => m.RemoveFromRolesAsync(_testUser,
            It.IsAny<IEnumerable<string>>()), Times.Once);
        _userManagerMock.Verify(m => m.AddToRolesAsync(_testUser,
            It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    /// <summary>
    /// Tester at fjerning av eksisterende roller feiler og vi returnerer InternalError
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenRemoveRolesFails_ReturnsInternalError()
    {
        // Arrange
        var request = new UpdateUserRequest { Roles = ["Admin"] };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(["Employee"]);

        _userManagerMock
            .Setup(m => m.RemoveFromRolesAsync(_testUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Unknown error" }));

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.InternalError);
        _userManagerMock.Verify(m => m.AddToRolesAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    /// <summary>
    /// Tester at tildeling av nye roller feiler og returnerer InternalError
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenAddRolesFails_ReturnsInternalError()
    {
        // Arrange
        var request = new UpdateUserRequest { Roles = ["Admin"] };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(["Employee"]);

        _userManagerMock
            .Setup(m => m.RemoveFromRolesAsync(_testUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRolesAsync(_testUser, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Unknown error" }));

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.InternalError);
    }

    /// <summary>
    /// Tester at UpdateUserAsync returnerer NotFound når brukeren ikke eksisterer
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateUserRequest { FirstName = "Nytt" };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(Guid.NewGuid(), request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tester at eposten allerede er i bruk hos en annen bruker
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenEmailIsTaken_ReturnsConflict()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "Nytt",
            LastName = "Navn",
            JobTitleId = Guid.NewGuid(),
            Email = "ny@example.com"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Tester at manager ikke kan være brukeren
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_ManagerIdIsUserId_ReturnsConflict()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "Nytt",
            LastName = "Navn",
            JobTitleId = Guid.NewGuid(),
            Email = "ny@example.com",
            ManagerId = _testUser.Id
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Tester at avdeling ikke eksisterer
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenDepartmentDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "Nytt",
            LastName = "Navn",
            JobTitleId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid()
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _departmentRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Department, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Tester at manager ikke eksisterer
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WhenManagerDoesNotExist_ReturnsValidationError()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "Nytt",
            LastName = "Navn",
            JobTitleId = Guid.NewGuid(),
            ManagerId = Guid.NewGuid()
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userRepositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<UserDto> result = await _sut.UpdateUserAsync(_testUser.Id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ApplicationUser>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}