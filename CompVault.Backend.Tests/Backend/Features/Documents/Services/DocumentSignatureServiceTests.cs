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

public class DocumentSignatureServiceTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentSignatureRepository> _signatureRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IDocumentTargetingService> _targetingServiceMock;
    private readonly Mock<ILogger<DocumentSignatureService>> _loggerMock;
    private readonly DocumentSignatureService _sut;

    public DocumentSignatureServiceTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _signatureRepositoryMock = new Mock<IDocumentSignatureRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _targetingServiceMock = new Mock<IDocumentTargetingService>();
        _loggerMock = new Mock<ILogger<DocumentSignatureService>>();

        // Default: all access checks pass
        _targetingServiceMock
            .Setup(x => x.CheckAccessAsync(
                It.IsAny<Document>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = new DocumentSignatureService(
            _documentRepositoryMock.Object,
            _signatureRepositoryMock.Object,
            _userRepositoryMock.Object,
            _targetingServiceMock.Object,
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
            AllowedMimeTypes = ["application/pdf"]
        };
    }

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
    // SignAsync
    // -------------------------------------------------------------------------

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

    [Fact]
    public async Task SignAsync_AlreadySigned_ReturnsConflictFailure()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            Version = 1,
            RequiresSignature = true
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

    [Fact]
    public async Task SignAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var document = new Document
        {
            Id = docId,
            Version = 2,
            RequiresSignature = true
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
    // GetSignaturesAsync
    // -------------------------------------------------------------------------

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
                Title = "Avdelingsdok", Version = 1, IsActive = true,
                RequiresSignature = true
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
                Title = "Lederdok", Version = 1, IsActive = true,
                RequiresSignature = true
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

        var documents = new List<Document>
        {
            new()
            {
                Id = signedDocId, DocumentTypeId = typeId,
                DocumentType = type,
                Title = "Allerede signert", Version = 1, IsActive = true,
                RequiresSignature = true
            },
            new()
            {
                Id = unsignedDocId, DocumentTypeId = typeId,
                DocumentType = type,
                Title = "Usignert", Version = 1, IsActive = true,
                RequiresSignature = true
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
}
