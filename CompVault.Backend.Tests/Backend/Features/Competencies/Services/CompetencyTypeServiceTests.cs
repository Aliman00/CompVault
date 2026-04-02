using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Features.Competencies.Services;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Shared.DTOs.CompetencyTypes;
using CompVault.Shared.Result;

using FluentAssertions;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Competencies.Services;

public class CompetencyTypeServiceTests
{
    private readonly Mock<ICompetencyTypeRepository> _competencyTypeRepositoryMock;
    private readonly CompetencyTypeService _sut;

    public CompetencyTypeServiceTests()
    {
        _competencyTypeRepositoryMock = new Mock<ICompetencyTypeRepository>();
        _sut = new CompetencyTypeService(_competencyTypeRepositoryMock.Object);
    }

    // -------------------------------------------------------------------------
    // CreateAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at CreateAsync returnerer failure ved duplikat navn (case-insensitive).
    /// </summary>
    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var request = new CreateCompetencyTypeRequest { Name = "Førerkort B" };

        CompetencyType existingType = new() { Id = Guid.NewGuid(), Name = "FØRERKORT B" }; // Same name, different case

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType);

        // Act
        Result<CompetencyTypeDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester happy path for CreateAsync.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateCompetencyTypeRequest
        {
            Name = "Førerkort B",
            Description = "Klassisk bilførerkort",
            Category = "Sertifikat",
            RequiresExpiration = true
        };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetencyType?)null);

        CompetencyType? capturedType = null;
        _competencyTypeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CompetencyType>(), It.IsAny<CancellationToken>()))
            .Callback<CompetencyType, CancellationToken>((t, _) => capturedType = t);

        // Act
        Result<CompetencyTypeDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(request.Name);
        result.Value.Description.Should().Be(request.Description);
        result.Value.Category.Should().Be(request.Category);
        result.Value.RequiresExpiration.Should().Be(request.RequiresExpiration);
        result.Value.IsActive.Should().BeTrue();

        _competencyTypeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<CompetencyType>(), It.IsAny<CancellationToken>()), Times.Once);
        _competencyTypeRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når typen ikke finnes.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CompetencyTypeNotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyTypeRequest { Name = "New Name" };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetencyType?)null);

        // Act
        Result<CompetencyTypeDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at UpdateAsync returnerer failure ved duplikat navn (case-insensitive).
    /// </summary>
    [Fact]
    public async Task UpdateAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyTypeRequest { Name = "New Name" };

        CompetencyType existingType = new() { Id = Guid.NewGuid(), Name = "NEW NAME" };
        CompetencyType typeToUpdate = new() { Id = id, Name = "Old Name" };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeToUpdate);

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType);

        // Act
        Result<CompetencyTypeDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når man prøver å endre RequiresExpiration 
    /// på en type som har aktive competencies.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ChangeRequiresExpirationWithActiveCompetencies_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyTypeRequest { RequiresExpiration = false }; // Endrer fra true til false

        CompetencyType typeToUpdate = new()
        {
            Id = id,
            Name = "Test Type",
            RequiresExpiration = true
        };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeToUpdate);

        _competencyTypeRepositoryMock
            .Setup(x => x.HasCompetenciesAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Har aktive competencies

        // Act
        Result<CompetencyTypeDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
    }

    /// <summary>
    /// Tester happy path for UpdateAsync.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyTypeRequest
        {
            Name = "Updated Name",
            Description = "Updated Description",
            IsActive = false
        };

        CompetencyType typeToUpdate = new()
        {
            Id = id,
            Name = "Old Name",
            Description = "Old Description",
            IsActive = true,
            RequiresExpiration = true
        };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(typeToUpdate);

        // Act
        Result<CompetencyTypeDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be(request.Name);
        result.Value.Description.Should().Be(request.Description);
        result.Value.IsActive.Should().BeFalse();

        _competencyTypeRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at DeleteAsync returnerer failure når typen ikke finnes.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_CompetencyTypeNotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetencyType?)null);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at DeleteAsync returnerer failure når typen har aktive competencies.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_HasActiveCompetencies_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        CompetencyType type = new() { Id = id, Name = "Test Type" };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _competencyTypeRepositoryMock
            .Setup(x => x.HasCompetenciesAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
    }

    /// <summary>
    /// Tester at DeleteAsync kan slette type når det bare er archived/expired/revoked competencies.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_OnlyArchivedCompetencies_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        CompetencyType type = new() { Id = id, Name = "Test Type" };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _competencyTypeRepositoryMock
            .Setup(x => x.HasCompetenciesAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Ingen aktive competencies

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _competencyTypeRepositoryMock.Verify(x => x.SoftDeleteAsync(type, It.IsAny<CancellationToken>()), Times.Once);
        _competencyTypeRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tester at DeleteAsync kan slette type uten competencies.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_NoCompetencies_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        CompetencyType type = new() { Id = id, Name = "Test Type" };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _competencyTypeRepositoryMock
            .Setup(x => x.HasCompetenciesAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }
}
