using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Features.Documents.Services;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Documents.Services;

public class DocumentVersioningServiceTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentTypeRepository> _documentTypeRepositoryMock;
    private readonly Mock<IDocumentSignatureRepository> _signatureRepositoryMock;
    private readonly Mock<IDocumentTargetingService> _targetingServiceMock;
    private readonly Mock<IDocumentFileService> _fileServiceMock;
    private readonly Mock<ILogger<DocumentVersioningService>> _loggerMock;
    private readonly DocumentVersioningService _sut;

    public DocumentVersioningServiceTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentTypeRepositoryMock = new Mock<IDocumentTypeRepository>();
        _signatureRepositoryMock = new Mock<IDocumentSignatureRepository>();
        _targetingServiceMock = new Mock<IDocumentTargetingService>();
        _fileServiceMock = new Mock<IDocumentFileService>();
        _loggerMock = new Mock<ILogger<DocumentVersioningService>>();

        // Default: all access checks pass
        _targetingServiceMock
            .Setup(x => x.CheckAccessAsync(
                It.IsAny<Document>(), It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _sut = new DocumentVersioningService(
            _documentRepositoryMock.Object,
            _documentTypeRepositoryMock.Object,
            _signatureRepositoryMock.Object,
            _targetingServiceMock.Object,
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
            AllowedMimeTypes = ["application/pdf"]
        };
    }

    // -------------------------------------------------------------------------
    // GetDownloadAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDownloadAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<DocumentDownloadResult> result = await _sut.GetDownloadAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

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
    // UploadVersionAsync
    // -------------------------------------------------------------------------

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
            .ReturnsAsync(new Document
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

    [Fact]
    public async Task UploadVersionAsync_DisallowedMimeType_ReturnsValidationError()
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

    [Fact]
    public async Task UploadVersionAsync_FileTooLarge_ReturnsValidationError()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        DocumentType type = CreateDocumentType(DocumentTargetMode.None, typeId);
        type.StorageFolder = "test-folder";
        type.MaxFileSizeBytes = 10;
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

        var largeStream = new MemoryStream(new byte[100]);

        // Act
        Result<DocumentDto> result = await _sut.UploadVersionAsync(
            docId, "test-type", "large.pdf", "application/pdf", largeStream, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Contain("for stor");
    }

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
            .ReturnsAsync(("test-folder/active/doc/file_v2_tmp.pdf", "samechecksum"));

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

        _fileServiceMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

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