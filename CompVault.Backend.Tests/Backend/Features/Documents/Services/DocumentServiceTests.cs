using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Documents.Services;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
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
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IJobTitleRepository> _jobTitleRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IDocumentFileService> _fileServiceMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _signatureRepositoryMock = new Mock<IDocumentSignatureRepository>();
        _documentTypeRepositoryMock = new Mock<IDocumentTypeRepository>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _jobTitleRepositoryMock = new Mock<IJobTitleRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _fileServiceMock = new Mock<IDocumentFileService>();
        _loggerMock = new Mock<ILogger<DocumentService>>();

        _sut = new DocumentService(
            _documentRepositoryMock.Object,
            _signatureRepositoryMock.Object,
            _documentTypeRepositoryMock.Object,
            _departmentRepositoryMock.Object,
            _jobTitleRepositoryMock.Object,
            _userRepositoryMock.Object,
            _fileServiceMock.Object,
            _loggerMock.Object);
    }

    // Hjelpemetode for å opprette en gyldig DocumentType
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

    // Hjelpemetode for å opprette en bruker
    private static ApplicationUser CreateUser(
        Guid? id = null, Guid? departmentId = null, Guid? jobTitleId = null)
    {
        return new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Bruker",
            Email = "test@test.no",
            DepartmentId = departmentId,
            JobTitleId = jobTitleId
        };
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetAllAsync returnerer failure når dokumenttypen ikke finnes.
    /// </summary>
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

    /// <summary>
    /// Tester at GetAllAsync returnerer tom liste når ingen dokumenter finnes.
    /// </summary>
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

    /// <summary>
    /// Tester at GetAllAsync returnerer dokumenter med signaturinformasjon.
    /// </summary>
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

    /// <summary>
    /// Tester at GetByIdAsync returnerer failure når dokumentet ikke finnes.
    /// </summary>
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

    /// <summary>
    /// Tester at GetByIdAsync returnerer korrekt DTO.
    /// </summary>
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

    /// <summary>
    /// Tester at CreateAsync returnerer failure når dokumenttypen ikke finnes.
    /// </summary>
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
            "nonexistent", request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når TargetMode er Department
    /// men TargetDepartmentIds mangler.
    /// </summary>
    [Fact]
    public async Task CreateAsync_DepartmentModeWithoutTarget_ReturnsValidationFailure()
    {
        // Arrange
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department);
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [] // Tom liste for Department-modus
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når TargetMode er None
    /// men target-lister er satt.
    /// </summary>
    [Fact]
    public async Task CreateAsync_NoneModeWithTarget_ReturnsValidationFailure()
    {
        // Arrange
        DocumentType type = CreateDocumentType(DocumentTargetMode.None);
        var request = new CreateDocumentRequest
        {
            Title = "Test",
            TargetDepartmentIds = [Guid.NewGuid()] // Ikke tillatt for None
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når avdelingen ikke finnes.
    /// </summary>
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

        _departmentRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>().AsReadOnly());

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at CreateAsync returnerer failure når kategorien ikke tilhører
    /// riktig dokumenttype.
    /// </summary>
    [Fact]
    public async Task CreateAsync_InvalidCategory_ReturnsFailure()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None);
        type.Categories = []; // Tom liste — kategorien finnes ikke

        var request = new CreateDocumentRequest
        {
            Title = "Test",
            DocumentTypeCategoryId = categoryId
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesAsync(type.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        // Act
        Result<DocumentDto> result = await _sut.CreateAsync(
            "test-type", request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester happy path for CreateAsync uten filvedlegg.
    /// </summary>
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
            "test-type", request, uploadedById);

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

    /// <summary>
    /// Tester at CreateAsync med fil lagrer filen og setter filmetadata.
    /// </summary>
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
            "test.pdf", "application/pdf", stream);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _fileServiceMock.Verify(
            x => x.SaveWithChecksumAsync(It.IsAny<Stream>(),
                It.Is<string>(p => p.Contains("active")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når dokumentet ikke finnes.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<DocumentDto> result = await _sut.UpdateAsync(id, new UpdateDocumentRequest());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at UpdateAsync oppdaterer tittel.
    /// </summary>
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
            id, new UpdateDocumentRequest { Title = "Ny tittel" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        document.Title.Should().Be("Ny tittel");
    }

    /// <summary>
    /// Tester at UpdateAsync kan fjerne mål-avdelinger med tom liste.
    /// </summary>
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
            id, new UpdateDocumentRequest { TargetDepartmentIds = [] });

        // Assert
        result.IsSuccess.Should().BeTrue();
        document.DocumentDepartments.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at DeleteAsync returnerer failure når dokumentet ikke finnes.
    /// </summary>
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

    /// <summary>
    /// Tester at DeleteAsync soft-sletter dokumentet.
    /// </summary>
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

    // -------------------------------------------------------------------------
    // SignAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at SignAsync returnerer failure når dokumentet ikke finnes.
    /// </summary>
    [Fact]
    public async Task SignAsync_DocumentNotFound_ReturnsFailure()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _documentRepositoryMock
            .Setup(x => x.GetCurrentWithSignaturesAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<bool> result = await _sut.SignAsync(docId, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at SignAsync returnerer conflict når brukeren allerede har signert.
    /// </summary>
    [Fact]
    public async Task SignAsync_AlreadySigned_ReturnsConflictFailure()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            Version = 1
        };

        _documentRepositoryMock
            .Setup(x => x.GetCurrentWithSignaturesAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _signatureRepositoryMock
            .Setup(x => x.HasUserSignedVersionAsync(docId, userId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<bool> result = await _sut.SignAsync(docId, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
    }

    /// <summary>
    /// Tester happy path for SignAsync.
    /// </summary>
    [Fact]
    public async Task SignAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            Version = 2
        };

        _documentRepositoryMock
            .Setup(x => x.GetCurrentWithSignaturesAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _signatureRepositoryMock
            .Setup(x => x.HasUserSignedVersionAsync(docId, userId, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _signatureRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DocumentSignature>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentSignature s, CancellationToken _) => s);

        _signatureRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<bool> result = await _sut.SignAsync(docId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _signatureRepositoryMock.Verify(
            x => x.AddAsync(It.Is<DocumentSignature>(s =>
                s.DocumentId == docId &&
                s.UserId == userId &&
                s.SignatureVersion == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetDownloadAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetDownloadAsync returnerer failure når dokumentet ikke finnes.
    /// </summary>
    [Fact]
    public async Task GetDownloadAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<DocumentDownloadResult> result = await _sut.GetDownloadAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at GetDownloadAsync returnerer failure når dokumentet ikke har fil.
    /// </summary>
    [Fact]
    public async Task GetDownloadAsync_NoFile_ReturnsValidationFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var document = new Document
        {
            Id = id,
            FilePath = null,
            FileName = null
        };

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        Result<DocumentDownloadResult> result = await _sut.GetDownloadAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    /// <summary>
    /// Tester at GetDownloadAsync returnerer failure når filen mangler på lagring.
    /// </summary>
    [Fact]
    public async Task GetDownloadAsync_FileNotFoundOnStorage_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        var document = new Document
        {
            Id = id,
            FilePath = "/some/path.pdf",
            FileName = "test.pdf"
        };

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileServiceMock
            .Setup(x => x.ExistsAsync("/some/path.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<DocumentDownloadResult> result = await _sut.GetDownloadAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester happy path for GetDownloadAsync.
    /// </summary>
    [Fact]
    public async Task GetDownloadAsync_FileExists_ReturnsStream()
    {
        // Arrange
        var id = Guid.NewGuid();
        var document = new Document
        {
            Id = id,
            FilePath = "/files/test.pdf",
            FileName = "test.pdf",
            MimeType = "application/pdf",
            FileSize = 1024
        };

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileServiceMock
            .Setup(x => x.ExistsAsync("/files/test.pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<DocumentDownloadResult> result = await _sut.GetDownloadAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.FileName.Should().Be("test.pdf");
        result.Value?.ContentType.Should().Be("application/pdf");
        result.Value?.FilePath.Should().Be("/files/test.pdf");
    }

    // -------------------------------------------------------------------------
    // GetSignaturesAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetSignaturesAsync returnerer failure når dokumentet ikke finnes.
    /// </summary>
    [Fact]
    public async Task GetSignaturesAsync_DocumentNotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<IReadOnlyList<DocumentSignatureDto>> result =
            await _sut.GetSignaturesAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    // -------------------------------------------------------------------------
    // GetMySignedDocumentsAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetMySignedDocumentsAsync returnerer tom liste når brukeren
    /// ikke har signert noen dokumenter.
    /// </summary>
    [Fact]
    public async Task GetMySignedDocumentsAsync_NoSignatures_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _signatureRepositoryMock
            .Setup(x => x.GetSignedDocumentIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetMySignedDocumentsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // GetMyPendingDocumentsAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetMyPendingDocumentsAsync returnerer failure når brukeren
    /// ikke finnes.
    /// </summary>
    [Fact]
    public async Task GetMyPendingDocumentsAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetMyPendingDocumentsAsync(userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at GetMyPendingDocumentsAsync bruker batch-metoden
    /// GetPendingForUserAsync og returnerer korrekte dokumenter.
    /// </summary>
    [Fact]
    public async Task GetMyPendingDocumentsAsync_DepartmentMode_ReturnsPendingDocs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        ApplicationUser user = CreateUser(userId, departmentId);

        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.Department, typeId);

        var docId = Guid.NewGuid();
        var documents = new List<Document>
        {
            new()
            {
                Id = docId, DocumentTypeId = typeId,
                DocumentType = type,
                Title = "Avdelingsdok", Version = 1, IsActive = true
            }
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _signatureRepositoryMock
            .Setup(x => x.GetSignedDocumentIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _documentRepositoryMock
            .Setup(x => x.GetPendingForUserAsync(
                userId, departmentId, It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _signatureRepositoryMock
            .Setup(x => x.GetByDocumentIdsAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentSignature> { new() { DocumentId = docId, SignatureVersion = 1 } });

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetMyPendingDocumentsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].SignedByCurrentUser.Should().BeFalse();
    }

    /// <summary>
    /// Tester at GetMyPendingDocumentsAsync henter dokumenter for brukerens jobbtittel.
    /// </summary>
    [Fact]
    public async Task GetMyPendingDocumentsAsync_JobTitleMode_ReturnsPendingDocs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var jobTitleId = Guid.NewGuid();
        ApplicationUser user = CreateUser(userId, jobTitleId: jobTitleId);

        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.JobTitle, typeId);

        var docId = Guid.NewGuid();
        var documents = new List<Document>
        {
            new()
            {
                Id = docId, DocumentTypeId = typeId,
                DocumentType = type,
                Title = "Lederdok", Version = 1, IsActive = true
            }
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _signatureRepositoryMock
            .Setup(x => x.GetSignedDocumentIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _documentRepositoryMock
            .Setup(x => x.GetPendingForUserAsync(
                userId, It.IsAny<Guid?>(), jobTitleId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _signatureRepositoryMock
            .Setup(x => x.GetByDocumentIdsAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetMyPendingDocumentsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("Lederdok");
    }

    /// <summary>
    /// Tester at GetMyPendingDocumentsAsync filtrerer bort dokumenter
    /// brukeren allerede har signert.
    /// </summary>
    [Fact]
    public async Task GetMyPendingDocumentsAsync_AlreadySigned_FiltersOutSigned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        ApplicationUser user = CreateUser(userId);

        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None, typeId);

        var signedDocId = Guid.NewGuid();
        var unsignedDocId = Guid.NewGuid();

        // Repo returnerer begge dokumenter; service filtrerer bort det allerede signerte
        var documents = new List<Document>
        {
            new()
            {
                Id = signedDocId, DocumentTypeId = typeId,
                DocumentType = type,
                Title = "Allerede signert", Version = 1, IsActive = true
            },
            new()
            {
                Id = unsignedDocId, DocumentTypeId = typeId,
                DocumentType = type,
                Title = "Usignert", Version = 1, IsActive = true
            }
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _signatureRepositoryMock
            .Setup(x => x.GetSignedDocumentIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([signedDocId]);

        _documentRepositoryMock
            .Setup(x => x.GetPendingForUserAsync(
                userId, It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        _signatureRepositoryMock
            .Setup(x => x.GetByDocumentIdsAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Result<IReadOnlyList<DocumentListDto>> result =
            await _sut.GetMyPendingDocumentsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("Usignert");
    }

    // -------------------------------------------------------------------------
    // UploadVersionAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UploadVersionAsync laster opp fil og oppdaterer dokumentmetadata.
    /// </summary>
    [Fact]
    public async Task UploadVersionAsync_ValidFile_UpdatesDocumentAndArchivesOld()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None, typeId);
        type.StorageFolder = "test-folder";
        type.MaxFileSizeBytes = 50 * 1024 * 1024;
        type.AllowedMimeTypes = ["application/pdf"];

        var document = new Document
        {
            Id = docId,
            DocumentTypeId = typeId,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            IsActive = true,
            FileName = "old.pdf",
            FilePath = "test-folder/active/doc/file_v1.pdf",
            FileSize = 1024,
            MimeType = "application/pdf",
            Checksum = "oldsum"
        };

        var userId = Guid.NewGuid();
        var stream = new MemoryStream([1, 2, 3, 4, 5]);

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileServiceMock
            .Setup(x => x.ValidateMimeType("application/pdf", type.AllowedMimeTypes))
            .Returns(Result.Success());

        _fileServiceMock
            .Setup(x => x.ValidateFileSize(It.IsAny<long>(), type.MaxFileSizeBytes))
            .Returns(Result.Success());

        _fileServiceMock
            .Setup(x => x.SaveWithChecksumAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("test-folder/active/doc/file_v2_tmp.pdf", "newsum"));

        _fileServiceMock
            .Setup(x => x.MoveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _signatureRepositoryMock
            .Setup(x => x.GetForDocumentAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentSignature>());

        _documentRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new Document
            {
                Id = docId,
                DocumentTypeId = typeId,
                DocumentType = type,
                Title = "Test",
                Version = 2,
                IsActive = true,
                FileName = "new.pdf",
                FilePath = "test-folder/active/doc/file_v2.pdf",
                FileSize = 5,
                MimeType = "application/pdf",
                Checksum = "newsum"
            });

        // Act
        Result<DocumentDto> result = await _sut.UploadVersionAsync(
            docId, "test-type", "new.pdf", "application/pdf", stream, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Version.Should().Be(2);

        _fileServiceMock.Verify(
            x => x.MoveAsync(
                "test-folder/active/doc/file_v1.pdf",
                It.Is<string>(p => p.Contains("archived")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _signatureRepositoryMock.Verify(
            x => x.GetForDocumentAsync(docId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tester at UploadVersionAsync returnerer NotFound når dokument ikke finnes.
    /// </summary>
    [Fact]
    public async Task UploadVersionAsync_DocumentNotFound_ReturnsNotFound()
    {
        // Arrange
        var docId = Guid.NewGuid();

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<DocumentDto> result = await _sut.UploadVersionAsync(
            docId, "test-type", "new.pdf", "application/pdf", new MemoryStream([1]), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at UploadVersionAsync avviser MIME-type som ikke er tillatt for dokumenttypen.
    /// </summary>
    [Fact]
    public async Task UploadVersionAsync_DisallowedMimeType_ReturnsValidationError()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None, typeId);
        type.StorageFolder = "test-folder";
        type.AllowedMimeTypes = ["application/pdf"]; // Ikke text/plain

        var document = new Document
        {
            Id = docId,
            DocumentTypeId = typeId,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            IsActive = true
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileServiceMock
            .Setup(x => x.ValidateMimeType("text/plain", type.AllowedMimeTypes))
            .Returns(Result.Failure(AppError.Create(ErrorCode.Validation,
                "Filtypen 'text/plain' er ikke tillatt for denne dokumenttypen.")));

        // Act
        Result<DocumentDto> result = await _sut.UploadVersionAsync(
            docId, "test-type", "malicious.txt", "text/plain", new MemoryStream([1]), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Contain("ikke tillatt");
    }

    /// <summary>
    /// Tester at UploadVersionAsync avviser fil som er for stor for dokumenttypen.
    /// </summary>
    [Fact]
    public async Task UploadVersionAsync_FileTooLarge_ReturnsValidationError()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None, typeId);
        type.StorageFolder = "test-folder";
        type.MaxFileSizeBytes = 10; // Max 10 bytes
        type.AllowedMimeTypes = ["application/pdf"];

        var document = new Document
        {
            Id = docId,
            DocumentTypeId = typeId,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            IsActive = true
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileServiceMock
            .Setup(x => x.ValidateMimeType("application/pdf", type.AllowedMimeTypes))
            .Returns(Result.Success());

        _fileServiceMock
            .Setup(x => x.ValidateFileSize(100, type.MaxFileSizeBytes))
            .Returns(Result.Failure(AppError.Create(ErrorCode.Validation,
                "Filen er for stor. Maks tillatt størrelse: 0MB.")));

        var largeStream = new MemoryStream(new byte[100]); // 100 bytes

        // Act
        Result<DocumentDto> result = await _sut.UploadVersionAsync(
            docId, "test-type", "large.pdf", "application/pdf", largeStream, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Contain("for stor");
    }

    /// <summary>
    /// Tester at UploadVersionAsync avviser fil med identisk checksum som forrige versjon.
    /// </summary>
    [Fact]
    public async Task UploadVersionAsync_IdenticalChecksum_ReturnsValidationError()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None, typeId);
        type.StorageFolder = "test-folder";
        type.AllowedMimeTypes = ["application/pdf"];

        var document = new Document
        {
            Id = docId,
            DocumentTypeId = typeId,
            DocumentType = type,
            Title = "Test",
            Version = 1,
            IsActive = true,
            Checksum = "samechecksum"
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("test-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileServiceMock
            .Setup(x => x.ValidateMimeType("application/pdf", type.AllowedMimeTypes))
            .Returns(Result.Success());

        _fileServiceMock
            .Setup(x => x.ValidateFileSize(It.IsAny<long>(), It.IsAny<long>()))
            .Returns(Result.Success());

        _fileServiceMock
            .Setup(x => x.SaveWithChecksumAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("test-folder/active/doc/file_v2_tmp.pdf", "samechecksum")); // Samme som dokumentets nåværende

        _fileServiceMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<DocumentDto> result = await _sut.UploadVersionAsync(
            docId, "test-type", "same.pdf", "application/pdf", new MemoryStream([1]), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Contain("identisk med forrige versjon");

        // Temp-fil skal være slettet
        _fileServiceMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Tester at UploadVersionAsync bekrefter at slug matcher dokumentets faktiske type.
    /// </summary>
    [Fact]
    public async Task UploadVersionAsync_SlugMismatch_ReturnsNotFound()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var wrongTypeId = Guid.NewGuid();

        DocumentType correctType = CreateDocumentType(DocumentTargetMode.None, typeId);
        DocumentType wrongType = CreateDocumentType(DocumentTargetMode.None, wrongTypeId);
        wrongType.Slug = "wrong-type";

        var document = new Document
        {
            Id = docId,
            DocumentTypeId = wrongTypeId,
            DocumentType = wrongType,
            Title = "Test",
            Version = 1,
            IsActive = true
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync("correct-type", It.IsAny<CancellationToken>()))
            .ReturnsAsync(correctType);

        _documentRepositoryMock
            .Setup(x => x.GetForUpdateAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        Result<DocumentDto> result = await _sut.UploadVersionAsync(
            docId, "correct-type", "file.pdf", "application/pdf", new MemoryStream([1]), Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
        result.Error.Message.Should().Contain("ikke av dokumenttype");
    }
}
