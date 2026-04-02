using System.Linq.Expressions;

using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Competencies.Services;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

using FluentAssertions;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Competencies.Services;

public class CompetencyServiceTests
{
    private readonly Mock<ICompetencyRepository> _competencyRepositoryMock;
    private readonly Mock<ICompetencyTypeRepository> _competencyTypeRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly CompetencyService _sut;

    public CompetencyServiceTests()
    {
        _competencyRepositoryMock = new Mock<ICompetencyRepository>();
        _competencyTypeRepositoryMock = new Mock<ICompetencyTypeRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _sut = new CompetencyService(
            _competencyRepositoryMock.Object,
            _competencyTypeRepositoryMock.Object,
            _userRepositoryMock.Object);
    }

    // -------------------------------------------------------------------------
    // CreateAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at CreateAsync returnerer failure når brukeren ikke finnes.
    /// </summary>
    [Fact]
    public async Task CreateAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new CreateCompetencyRequest
        {
            UserId = Guid.NewGuid(),
            CompetencyTypeId = Guid.NewGuid(),
            IssuedDate = DateTime.UtcNow.AddDays(-10)
        };

        CompetencyType type = new() { Id = request.CompetencyTypeId.Value, Name = "Test", IsActive = true, RequiresExpiration = false };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(request.CompetencyTypeId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(u => u.Id == request.UserId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<CompetencyDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når kompetansetypen ikke finnes.
    /// </summary>
    [Fact]
    public async Task CreateAsync_CompetencyTypeNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new CreateCompetencyRequest
        {
            UserId = Guid.NewGuid(),
            CompetencyTypeId = Guid.NewGuid(),
            IssuedDate = DateTime.UtcNow.AddDays(-10)
        };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(request.CompetencyTypeId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetencyType?)null);

        // Act
        Result<CompetencyDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når kompetansetypen er inaktiv.
    /// </summary>
    [Fact]
    public async Task CreateAsync_CompetencyTypeInactive_ReturnsFailure()
    {
        // Arrange
        var request = new CreateCompetencyRequest
        {
            UserId = Guid.NewGuid(),
            CompetencyTypeId = Guid.NewGuid(),
            IssuedDate = DateTime.UtcNow.AddDays(-10)
        };

        CompetencyType type = new() { Id = request.CompetencyTypeId.Value, Name = "Inactive Type", IsActive = false, RequiresExpiration = false };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(request.CompetencyTypeId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        // Act
        Result<CompetencyDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når typen krever utløpsdato men request mangler den.
    /// </summary>
    [Fact]
    public async Task CreateAsync_TypeRequiresExpirationButMissing_ReturnsFailure()
    {
        // Arrange
        var competencyTypeId = Guid.NewGuid();
        var request = new CreateCompetencyRequest
        {
            UserId = TestConstants.Users.ActiveUserId,
            CompetencyTypeId = competencyTypeId,
            IssuedDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = null // Mangler påkrevd utløpsdato
        };

        CompetencyType type = new() { Id = competencyTypeId, Name = "Requires Expiry", IsActive = true, RequiresExpiration = true };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<CompetencyDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når ExpiryDate er før IssuedDate.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ExpiryDateBeforeIssuedDate_ReturnsFailure()
    {
        // Arrange
        var competencyTypeId = Guid.NewGuid();
        var request = new CreateCompetencyRequest
        {
            UserId = TestConstants.Users.ActiveUserId,
            CompetencyTypeId = competencyTypeId,
            IssuedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(-10) // Expiry før Issued
        };

        CompetencyType type = new() { Id = competencyTypeId, Name = "Test", IsActive = true, RequiresExpiration = true };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<CompetencyDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester happy path for CreateAsync med gyldig request.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateCompetencyRequest
        {
            UserId = TestConstants.Users.ActiveUserId,
            CompetencyTypeId = Guid.NewGuid(),
            IssuedDate = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddDays(100),
            CertificateNumber = "CERT-123"
        };

        CompetencyType type = new()
        {
            Id = request.CompetencyTypeId.Value,
            Name = "Valid Type",
            IsActive = true,
            RequiresExpiration = true
        };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(request.CompetencyTypeId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _competencyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Competency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competency c, CancellationToken _) => c);

        _competencyRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var createdId = Guid.NewGuid();
        var returnCompetency = new Competency
        {
            Id = createdId,
            UserId = request.UserId.Value,
            CompetencyTypeId = request.CompetencyTypeId.Value,
            CompetencyType = type,
            IssuedDate = request.IssuedDate.Value,
            ExpiryDate = request.ExpiryDate,
            Status = CompetencyStatus.Valid,
            CertificateNumber = request.CertificateNumber
        };
        _competencyRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnCompetency);

        // Act
        Result<CompetencyDto> result = await _sut.CreateAsync(request);

        // Assert
        if (result.IsFailure)
        {
            result.Error.Should().BeNull("Expected success but got failure: " + result.Error!.Code + " - " + result.Error!.Message);
        }
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _competencyRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Competency>(), It.IsAny<CancellationToken>()), Times.Once);
        _competencyRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når kompetansen ikke finnes.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CompetencyNotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyRequest();

        _competencyRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competency?)null);

        // Act
        Result<CompetencyDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når ExpiryDate er før IssuedDate.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ExpiryDateBeforeIssuedDate_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyRequest
        {
            ExpiryDate = DateTime.UtcNow.AddDays(-10),
            IssuedDate = DateTime.UtcNow
        };

        CompetencyType type = new() { Id = Guid.NewGuid(), Name = "Test", RequiresExpiration = true };
        Competency competency = new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CompetencyTypeId = type.Id,
            CompetencyType = type,
            IssuedDate = DateTime.UtcNow.AddDays(-5),
            ExpiryDate = DateTime.UtcNow.AddDays(100)
        };

        _competencyRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competency);

        // Act
        Result<CompetencyDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når man prøver å sette ugyldig status.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_InvalidStatus_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyRequest
        {
            Status = CompetencyStatus.Expired // Kan ikke settes manuelt
        };

        CompetencyType type = new() { Id = Guid.NewGuid(), Name = "Test", RequiresExpiration = true };
        Competency competency = new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CompetencyTypeId = type.Id,
            CompetencyType = type,
            IssuedDate = DateTime.UtcNow.AddDays(-5),
            ExpiryDate = DateTime.UtcNow.AddDays(100),
            Status = CompetencyStatus.Valid
        };

        _competencyRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competency);

        // Act
        Result<CompetencyDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når Revoked uten grunn.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_RevokedWithoutReason_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyRequest
        {
            Status = CompetencyStatus.Revoked,
            RevokedReason = null // Mangler påkrevd grunn
        };

        CompetencyType type = new() { Id = Guid.NewGuid(), Name = "Test", RequiresExpiration = true };
        Competency competency = new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CompetencyTypeId = type.Id,
            CompetencyType = type,
            IssuedDate = DateTime.UtcNow.AddDays(-5),
            ExpiryDate = DateTime.UtcNow.AddDays(100),
            Status = CompetencyStatus.Valid
        };

        _competencyRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competency);

        // Act
        Result<CompetencyDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at DeleteAsync returnerer failure når kompetansen ikke finnes.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_CompetencyNotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();

        _competencyRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Competency?)null);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester happy path for DeleteAsync.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ValidId_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        Competency competency = new() { Id = id, UserId = Guid.NewGuid() };

        _competencyRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competency);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _competencyRepositoryMock.Verify(x => x.SoftDeleteAsync(competency, It.IsAny<CancellationToken>()), Times.Once);
        _competencyRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
