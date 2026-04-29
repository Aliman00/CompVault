using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Audit.Services;
using CompVault.Backend.Features.Competencies.Services;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Tests.Common;
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
    private readonly Mock<IAuditContext> _auditContextMock;
    private readonly Mock<IDepartmentScopeService> _departmentScopeMock;
    private readonly CompetencyService _sut;

    public CompetencyServiceTests()
    {
        _competencyRepositoryMock = new Mock<ICompetencyRepository>();
        _competencyTypeRepositoryMock = new Mock<ICompetencyTypeRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditContextMock = new Mock<IAuditContext>();
        _departmentScopeMock = new Mock<IDepartmentScopeService>();
        
        // Mocker departmentScope til å tilatte alle kall for å ikke tenke på dette hvor vi ikke tester 
        // logikken rundt DepartmentScope
        _departmentScopeMock
            .Setup(s => s.IsAllowed(It.IsAny<Guid>(), 
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        
        _sut = new CompetencyService(
            _competencyRepositoryMock.Object,
            _competencyTypeRepositoryMock.Object,
            _userRepositoryMock.Object,
            _auditContextMock.Object,
            _departmentScopeMock.Object);
    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Oppretter en CompetencyRequest for testing
    /// </summary>
    /// <param name="userId">Brukerens ID. Default new Guid</param>
    /// <param name="competencyTypeId">ID til CompetencyType. Default new Guid</param>
    /// <param name="issuedDate">Utlevert data. Default 10 dager siden</param>
    /// <param name="expiryDate">Utgått dato. Default null</param>
    /// <param name="certificateNumber">Sertifikatnummer. Default null</param>
    /// <param name="notes">Notatetr. Default null</param>
    /// <returns>CreateCompetencyRequest klar til å teste på</returns>
    private static CreateCompetencyRequest CreateCompetencyRequest(
        Guid? userId = null,
        Guid? competencyTypeId = null,
        DateTime? issuedDate = null,
        DateTime? expiryDate = null,
        string? certificateNumber = null,
        string? notes = null) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        CompetencyTypeId = competencyTypeId ?? Guid.NewGuid(),
        IssuedDate = issuedDate ?? DateTime.UtcNow.AddDays(-10),
        ExpiryDate = expiryDate,
        CertificateNumber = certificateNumber,
        Notes = notes
    };
    
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
            .Setup(x => x.GetByIdIgnoringFiltersAsync(request.UserId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

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
        var targetUser = new ApplicationUser
        {
            Id = request.UserId!.Value, 
            IsActive = true, 
            DepartmentId = Guid.NewGuid()
        };
        
        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);
        
        _userRepositoryMock
            .Setup(x => x.GetByIdIgnoringFiltersAsync(request.UserId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

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
        var targetUser = new ApplicationUser
        {
            Id = request.UserId!.Value, 
            IsActive = true, 
            DepartmentId = Guid.NewGuid()
        };
        
        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

       
        _userRepositoryMock
            .Setup(x => x.GetByIdIgnoringFiltersAsync(request.UserId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

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
        
        // Bruker med avdeling for å sjekke tilattelse
        var targetUser = new ApplicationUser
        {
            Id = request.UserId.Value,
            IsActive = true,
            DepartmentId = Guid.NewGuid()
        };

        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(request.CompetencyTypeId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _userRepositoryMock
            .Setup(x => x.GetByIdIgnoringFiltersAsync(request.UserId.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);

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
    
    /// <summary>
    /// Tester at brukeren som tilknytter en brukers ansattkompetanse ikke er i samme avdeling og heller
    /// ikke har tilattelse
    /// </summary>
    [Fact]
    public async Task CreateAsync_UserInAnotherDepartment_ReturnsForbidden()
    {
        // Arrange
        CreateCompetencyRequest request = CreateCompetencyRequest();
        CompetencyType type = TestDataFactory.CreateCompetencyType(id: request.CompetencyTypeId!.Value, 
            requiresExpiration: false);
        ApplicationUser targetUser = TestDataFactory.CreateApplicationUser(id: request.UserId!.Value);
        
        // Mocker at vi henter korrekt Type
        _competencyTypeRepositoryMock
            .Setup(x => x.GetByIdAsync(request.CompetencyTypeId.Value, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);
        
        // Mocker at vi henter riktig bruker
        _userRepositoryMock
            .Setup(x => x.GetByIdIgnoringFiltersAsync(request.UserId.Value, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetUser);
        
        // Mcoker at vi får false og ikke har bypass
        _departmentScopeMock
            .Setup(s => s.IsAllowed(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        Result<CompetencyDto> result = await _sut.CreateAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Forbidden);
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
        var user = new ApplicationUser { Id = Guid.NewGuid(), DepartmentId = Guid.NewGuid() };
        
        Competency competency = new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CompetencyTypeId = type.Id,
            CompetencyType = type,
            ApplicationUser = user, 
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
        var user = new ApplicationUser { Id = Guid.NewGuid(), DepartmentId = Guid.NewGuid() };
        
        Competency competency = new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CompetencyTypeId = type.Id,
            CompetencyType = type,
            ApplicationUser = user,
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
        
        // Bruker med avdeling for å sjekke hierarkiet
        var user = new ApplicationUser { Id = Guid.NewGuid(), DepartmentId = Guid.NewGuid() };
        
        Competency competency = new()
        {
            Id = id,
            UserId = Guid.NewGuid(),
            CompetencyTypeId = type.Id,
            CompetencyType = type,
            ApplicationUser = user, 
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
    /// Mocker at en bruker prøvver å oppdatere en annen bruker sin ansattkompetanse, men de er i
    /// forskjellige avdelinger og ingen bypass.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_UserInAnotherDepartment_ReturnsForbidden()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCompetencyRequest();

        CompetencyType type = TestDataFactory.CreateCompetencyType();
        ApplicationUser user = TestDataFactory.CreateApplicationUser();

        Competency competency = TestDataFactory.CreateCompetency(
            userId: user.Id,
            competencyTypeId: type.Id);
        competency.CompetencyType = type;
        competency.ApplicationUser = user;
        
        // Mocker at vi henter korrekt kompetanse
        _competencyRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(competency);
        
        // Mocker at vi ikke har tilattelse
        _departmentScopeMock
            .Setup(s => s.IsAllowed(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        Result<CompetencyDto> result = await _sut.UpdateAsync(id, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Forbidden);
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
        var user = new ApplicationUser { Id = Guid.NewGuid(), DepartmentId = Guid.NewGuid() };
        Competency competency = new() { Id = id, UserId = user.Id }; 

        _competencyRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(competency);
        
        _userRepositoryMock
            .Setup(x => x.GetByIdIgnoringFiltersAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _competencyRepositoryMock.Verify(x => x.SoftDeleteAsync(competency, It.IsAny<CancellationToken>()), Times.Once);
        _competencyRepositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    /// <summary>
    /// Tester at en bruker i en avdeling ikke kan slette en bruker sitt kompetansebevis i en annen avdeling
    /// </summary>
    [Fact]
    public async Task DeleteAsync_UserInAnotherDepartment_ReturnsForbidden()
    {
        // Arrange
        var id = Guid.NewGuid();
        ApplicationUser user = TestDataFactory.CreateApplicationUser();
        Competency competency = TestDataFactory.CreateCompetency(userId: user.Id);
        
        // Mocker at vi henter riktig kompetanse
        _competencyRepositoryMock
            .Setup(x => x.GetByIdAsync(id, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(competency);
        
        // Mocker at vi henter riktig bruker
        _userRepositoryMock
            .Setup(x => x.GetByIdIgnoringFiltersAsync(user.Id, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        
        // Mocker at vi ikke har tilattelse
        _departmentScopeMock
            .Setup(s => s.IsAllowed(It.IsAny<Guid>(), 
                It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Forbidden);
    }
}