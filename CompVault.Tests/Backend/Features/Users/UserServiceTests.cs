using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Users;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;
using Moq;

namespace CompVault.Tests.Backend.Features.Users;

public class UserServiceTests
{
    // Mocker avhengighetene UserService trenger
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

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
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // UserManager krever en IUserStore-mock for å kunne instansieres
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        // UserManager har ingen tom konstruktør — IUserStore er eneste påkrevde parameter.
        // Resten settes til null! siden de ikke brukes i disse testene.
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _sut = new UserService(
            _userRepositoryMock.Object,
            _userManagerMock.Object,
            _unitOfWorkMock.Object);
    }

    /// <summary>
    /// Tester at GetUserByIdAsync returnerer brukeren når den eksisterer og er aktiv
    /// </summary>
    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(_testUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testUser);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(_testUser))
            .ReturnsAsync(new List<string> { "Employee" });

        // Act
        var result = await _sut.GetUserByIdAsync(_testUser.Id);

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
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(Guid.NewGuid());

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
        var result = await _sut.CreateUserAsync(request);

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
        var result = await _sut.CreateUserAsync(request);

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

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteUserAsync(_testUser.Id);

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
        var result = await _sut.DeleteUserAsync(Guid.NewGuid());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Error!.Code);
    }
}
