using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Documents.Services;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Documents.Services;

public class DocumentServiceTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentSignatureRepository> _signatureRepositoryMock;
    private readonly Mock<IDocumentTypeRepository> _documentTypeRepositoryMock;
    private readonly Mock<IDocumentTargetingService> _targetingServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IDocumentFileService> _fileServiceMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _signatureRepositoryMock = new Mock<IDocumentSignatureRepository>();
        _documentTypeRepositoryMock = new Mock<IDocumentTypeRepository>();
        _targetingServiceMock = new Mock<IDocumentTargetingService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _fileServiceMock = new Mock<IDocumentFileService>();
        _loggerMock = new Mock<ILogger<DocumentService>>();

        // Default: all targeting checks pass
        _targetingServiceMock
            .Setup(x => x.CheckAccessAsync(
                It.IsAny<Document>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _targetingServiceMock
            .Setup(x => x.CanUserAccessDocument(
                It.IsAny<Document>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .Returns(true);

        _targetingServiceMock
            .Setup(x => x.ValidateTarget(
                It.IsAny<DocumentType>(), It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(), It.IsAny<bool>()))
            .Returns(Result.Success());

        _targetingServiceMock
            .Setup(x => x.GetAndValidateDepartmentsExistAsync(
                It.IsAny<Guid>(), It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Department>>.Success(new List<Department>()));

        _targetingServiceMock
            .Setup(x => x.CheckDepartmentPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Department>>(),
                It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _targetingServiceMock
            .Setup(x => x.ValidateJobTitlesExistAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = new DocumentService(
            _documentRepositoryMock.Object,
            _signatureRepositoryMock.Object,
            _documentTypeRepositoryMock.Object,
            _targetingServiceMock.Object,
            _userRepositoryMock.Object,
            _fileServiceMock.Object,
            _loggerMock.Object);
    }

    private static DocumentType CreateDocumentType(
        DocumentTargetMode targetMode = DocumentTargetMode.None,
        Guid? id = null)
    {
        return new DocumentType
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test Type",
            Slug = "test-type",
            TargetMode = targetMode,
            IsActive = true,
            StorageFolder = "test-type",
            AllowedMimeTypes = new[]
            {
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "text/plain",
                "text/csv",
                "image/png",
                "image/jpeg"
            }
        };
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_DocumentTypeNotFound_ReturnsFailure()
    {
        // Arrange
        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetAllAsync("nonexistent", Guid.NewGuid(), null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task GetAllAsync_NoDocuments_ReturnsEmptyList()
    {
        // Arrange
        DocumentType type = CreateDocumentType();

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentRepositoryMock
            .Setup(x => x.GetByDocumentTypeAsync(type.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetAllAsync("test-type", Guid.NewGuid(), null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_HasDocuments_ReturnsWithSignatureInfo()
    {
        // Arrange
        DocumentType type = CreateDocumentType();
        var userId = Guid.NewGuid();
        var docId = Guid.NewGuid();

        var documents = new List<Document>
        {
            new()
            {
                Id = docId, DocumentTypeId = type.Id, DocumentType = type,
                Title = "Test dokument", Version = 1, IsActive = true
            }
        };

        var signatures = new List<DocumentSignature>
        {
            new()
            {
                DocumentId = docId, UserId = userId,
                SignatureVersion = 1
            }
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentRepositoryMock
            .Setup(x => x.GetByDocumentTypeAsync(type.Id, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _signatureRepositoryMock
            .Setup(x => x.GetByDocumentIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.Contains(docId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(signatures);

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetAllAsync("test-type", userId, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].TotalSignatures.Should().Be(1);
        result.Value[0].SignedByCurrentUser.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<DocumentDto> result = await _sut.GetByIdAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        DocumentType type = CreateDocumentType();
        var document = new Document
        {
            Id = id,
            DocumentTypeId = type.Id,
            DocumentType = type,
            Title = "Test dokument",
            Version = 1
        };

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        Result<DocumentDto> result = await _sut.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Id.Should().Be(id);
        result.Value?.Title.Should().Be("Test dokument");
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_DocumentTypeNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new CreateDocumentRequest { Title = "Test" };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "nonexistent", request, Guid.NewGuid(), bypassTarget: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task CreateAsync_DepartmentModeWithoutTarget_ReturnsValidationFailure()
    {
        // Arrange
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = []
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _targetingServiceMock
            .Setup(x => x.ValidateTarget(type, request.TargetDepartmentIds, request.TargetJobTitleIds, true))
            .Returns(Result.Failure(AppError.Create(ErrorCode.Validation,
                $"Dokumenttype '{type.Name}' krever minst én målavdeling (TargetDepartmentIds).")));

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid(), bypassTarget: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public async Task CreateAsync_NoneModeWithTarget_ReturnsValidationFailure()
    {
        // Arrange
        DocumentType type = CreateDocumentType(DocumentTargetMode.None);
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [Guid.NewGuid()]
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _targetingServiceMock
            .Setup(x => x.ValidateTarget(type, request.TargetDepartmentIds, request.TargetJobTitleIds, true))
            .Returns(Result.Failure(AppError.Create(ErrorCode.Validation,
                $"Dokumenttype '{type.Name}' har TargetMode=None. Target-lister kan ikke settes.")));

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid(), bypassTarget: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public async Task CreateAsync_DepartmentNotFound_ReturnsFailure()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [departmentId]
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _targetingServiceMock
            .Setup(x => x.GetAndValidateDepartmentsExistAsync(
                It.IsAny<Guid>(), request.TargetDepartmentIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Department>>.Failure(
                AppError.NotFound($"Avdeling med ID '{departmentId}' ble ikke funnet.")));

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid(), bypassTarget: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategory_ReturnsFailure()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None);
        type.Categories = [];

        var request = new CreateDocumentRequest
        {
            Title = "Test",
            DocumentTypeCategoryId = categoryId
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid(), bypassTarget: true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ValidRequestWithoutFile_ReturnsSuccess()
    {
        // Arrange
        DocumentType type = CreateDocumentType(DocumentTargetMode.None);
        var uploadedById = Guid.NewGuid();
        var request = new CreateDocumentRequest { Title = "Nytt dokument" };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document d, CancellationToken _) => d);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                new Document
                {
                    Id = id,
                    DocumentTypeId = type.Id,
                    DocumentType = type,
                    Title = request.Title,
                    Version = 1
                });

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, uploadedById, bypassTarget: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Title.Should().Be("Nytt dokument");

        _documentRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Document>(d =>
                d.Title == "Nytt dokument" &&
                d.DocumentTypeId == type.Id &&
                d.UploadedBy == uploadedById), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithFile_SavesFileAndSetsMetadata()
    {
        // Arrange
        DocumentType type = CreateDocumentType(DocumentTargetMode.None);
        var uploadedById = Guid.NewGuid();
        var request = new CreateDocumentRequest { Title = "Dok med fil" };
        var stream = new MemoryStream([1, 2, 3, 4, 5]);

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _fileServiceMock
            .Setup(x => x.ValidateMimeType("application/pdf", type.AllowedMimeTypes))
            .Returns(Result.Success());

        _fileServiceMock
            .Setup(x => x.ValidateFileSize(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(Result.Success());

        _fileServiceMock
            .Setup(x => x.SaveWithChecksumAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("saved/path", "sha256hash"));

        _documentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document d, CancellationToken _) => d);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                new Document
                {
                    Id = id,
                    DocumentTypeId = type.Id,
                    DocumentType = type,
                    Title = request.Title,
                    Version = 1
                });

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, uploadedById,
            true, "test.pdf", "application/pdf", stream);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fileServiceMock.Verify(
            x => x.SaveWithChecksumAsync(It.IsAny<Stream>(),
                It.Is<string>(p => p.Contains("active")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UserHasNoDepartment_ReturnsForbidden()
    {
        // Arrange
        var departmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        string slug = "hms";
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [departmentId]
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _targetingServiceMock
            .Setup(x => x.CheckDepartmentPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Department>>(),
                request.TargetDepartmentIds, It.IsAny<List<Guid>>(),
                false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                AppError.Create(ErrorCode.Forbidden, "Bruker har ingen tilknyttet avdeling")));

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            slug, request, Guid.NewGuid(), bypassTarget: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_ForbiddenDepartment_ReturnsForbiddenDepartment()
    {
        // Arrange
        var forbiddenDepartmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        string slug = "hms";
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [forbiddenDepartmentId]
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _targetingServiceMock
            .Setup(x => x.CheckDepartmentPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Department>>(),
                request.TargetDepartmentIds, It.IsAny<List<Guid>>(),
                false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                AppError.Create(ErrorCode.ForbiddenDepartment,
                    $"Du har ikke tilgang til følgende avdelinger: {forbiddenDepartmentId}")));

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            slug, request, Guid.NewGuid(), bypassTarget: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.ForbiddenDepartment);
    }

    [Fact]
    public async Task CreateAsync_AllowedDepartment_ReturnsSuccess()
    {
        // Arrange
        var childDepartmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        string slug = "hms";
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [childDepartmentId]
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        // Default mocks: all targeting checks pass

        _documentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document d, CancellationToken _) => d);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new Document
            {
                Id = id,
                DocumentTypeId = type.Id,
                DocumentType = type,
                Title = request.Title,
                Version = 1
            });

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            slug, request, Guid.NewGuid(), bypassTarget: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Title.Should().Be("Test");
    }

    [Fact]
    public async Task CreateAsync_BypassTarget_SkipsDepartmentAccessCheck()
    {
        // Arrange
        var otherDepartmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        string slug = "hms";
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [otherDepartmentId]
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        // Default mocks: all targeting checks pass (including CheckDepartmentPermissionAsync with bypass=true)

        _documentRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document d, CancellationToken _) => d);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new Document
            {
                Id = id,
                DocumentTypeId = type.Id,
                DocumentType = type,
                Title = request.Title,
                Version = 1
            });

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            slug, request, Guid.NewGuid(), bypassTarget: true);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify that CheckDepartmentPermissionAsync was called with bypassTarget=true
        _targetingServiceMock.Verify(
            x => x.CheckDepartmentPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Department>>(),
                request.TargetDepartmentIds, It.IsAny<List<Guid>>(),
                true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<DocumentDto> result = await _sut.UpdateAsync(id, Guid.NewGuid(), new UpdateDocumentRequest(), true);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTitle_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        DocumentType type = CreateDocumentType();
        var document = new Document
        {
            Id = id,
            DocumentTypeId = type.Id,
            DocumentType = type,
            Title = "Gammel tittel",
            Version = 1
        };

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        Result<DocumentDto> result = await _sut.UpdateAsync(
            id, Guid.NewGuid(), new UpdateDocumentRequest { Title = "Ny tittel" }, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        document.Title.Should().Be("Ny tittel");
    }

    [Fact]
    public async Task UpdateAsync_ClearsTargetDepartments_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType();
        var document = new Document
        {
            Id = id,
            DocumentTypeId = type.Id,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            DocumentDepartments = [new DocumentDepartment { DocumentId = id, DepartmentId = departmentId }]
        };

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act — tom liste fjerner alle mål-avdelinger
        Result<DocumentDto> result = await _sut.UpdateAsync(
            id, Guid.NewGuid(), new UpdateDocumentRequest { TargetDepartmentIds = [] }, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        document.DocumentDepartments.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ForbiddenDepartmentAdded_ReturnsForbiddenDepartment()
    {
        // Arrange
        var id = Guid.NewGuid();
        var forbiddenDepartmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        var document = new Document
        {
            Id = id,
            DocumentTypeId = type.Id,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            DocumentDepartments = []
        };
        var request = new UpdateDocumentRequest { TargetDepartmentIds = [forbiddenDepartmentId] };

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _targetingServiceMock
            .Setup(x => x.CheckDepartmentPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Department>>(),
                It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
                false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                AppError.Create(ErrorCode.ForbiddenDepartment,
                    $"Du har ikke tilgang til følgende avdelinger: {forbiddenDepartmentId}")));

        // Act
        Result<DocumentDto> result = await _sut.UpdateAsync(id, Guid.NewGuid(), request, bypassTarget: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.ForbiddenDepartment);
    }

    [Fact]
    public async Task UpdateAsync_ForbiddenDepartmentRemoved_ReturnsForbiddenDepartment()
    {
        // Arrange
        var id = Guid.NewGuid();
        var forbiddenDepartmentId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        var document = new Document
        {
            Id = id,
            DocumentTypeId = type.Id,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            DocumentDepartments =
            [
                new DocumentDepartment { DocumentId = id, DepartmentId = forbiddenDepartmentId }
            ]
        };
        var request = new UpdateDocumentRequest
        {
            TargetDepartmentIds = []
        };

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _targetingServiceMock
            .Setup(x => x.CheckDepartmentPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Department>>(),
                It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
                false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                AppError.Create(ErrorCode.ForbiddenDepartment,
                    $"Du har ikke tilgang til følgende avdelinger: {forbiddenDepartmentId}")));

        // Act
        Result<DocumentDto> result = await _sut.UpdateAsync(id, Guid.NewGuid(), request, bypassTarget: false);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.ForbiddenDepartment);
    }

    [Fact]
    public async Task UpdateAsync_BypassTarget_SkipsDepartmentAccessCheck()
    {
        // Arrange
        var id = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        var document = new Document
        {
            Id = id,
            DocumentTypeId = type.Id,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            DocumentDepartments = []
        };
        var request = new UpdateDocumentRequest
        {
            TargetDepartmentIds = []
        };

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        Result<DocumentDto> result = await _sut.UpdateAsync(id, Guid.NewGuid(), request, bypassTarget: true);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify that CheckDepartmentPermissionAsync was called with bypassTarget=true
        _targetingServiceMock.Verify(
            x => x.CheckDepartmentPermissionAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Department>>(),
                It.IsAny<List<Guid>>(), It.IsAny<List<Guid>>(),
                true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public async Task DeleteAsync_Found_SoftDeletesAndReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var document = new Document { Id = id, Title = "Test", IsActive = true };

        _documentRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentRepositoryMock
            .Setup(x => x.SoftDeleteAsync(document, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<bool> result = await _sut.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _documentRepositoryMock.Verify(
            x => x.SoftDeleteAsync(document, It.IsAny<CancellationToken>()), Times.Once);
        _documentRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}