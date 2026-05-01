using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Documents.Services;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.DTOs.Documents;
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

    private static ApplicationUser CreateUser(
        Guid? id = null, Guid? departmentId = null, Guid? jobTitleId = null)
    {
        return new ApplicationUser
        {
            Id = id ?? Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Bruker",
            Email = "test@test.no",
            DepartmentId = departmentId ?? TestConstants.Departments.DefaultDepartmentId,
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
    public async Task GetSignatureStatus_DocumentNotFound_ReturnsFailure()
    {
        // Arrange
        var id = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        Result<IReadOnlyList<UserSignatureStatusDto>> result =
            await _sut.GetSignatureStatusAsync(id);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at brukeren ikke riktig tilattelse eller er ikke i målgruppen
    /// </summary>
    [Fact]
    public async Task GetSignatureStatus_AccessDenied_ReturnsFailure()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var document = new Document { Id = docId, Version = 1 };

        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(docId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Mocker at vi får feilmelding når vi sjekker om vi har tilgang
        _targetingServiceMock
            .Setup(x => x.CheckAccessAsync(
                document, It.IsAny<Guid?>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(AppError.Create(ErrorCode.Forbidden,
                "Du har ikke tilgang til dette dokumentet.")));

        // Act
        Result<IReadOnlyList<UserSignatureStatusDto>> result =
            await _sut.GetSignatureStatusAsync(docId, currentUserId: Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tester happy path at vi henter dokumentet, brukeren har tilattelse og vi returner en bruker som har singert
    /// og en som ikke har signert. Tester for alle brukere
    /// </summary>
    [Fact]
    public async Task GetSignatureStatus_ValidDocument_ReturnsSignatureStatus()
    {
        // Arrange
        var docId = Guid.NewGuid();
        var signedUserId = Guid.NewGuid();
        var unsignedUserId = Guid.NewGuid();

        var document = new Document
        {
            Id = docId,
            Version = 2,
            DocumentDepartments = [],
            DocumentJobTitles = []
        };

        ApplicationUser signedUser = CreateUser(signedUserId);
        ApplicationUser unsignedUser = CreateUser(unsignedUserId);

        var signature = new DocumentSignature
        {
            UserId = signedUserId,
            DocumentId = docId,
            SignatureVersion = 2,
            SignedAt = DateTime.UtcNow
        };

        // Mocker at vi henter dokumentet
        _documentRepositoryMock
            .Setup(x => x.GetWithDetailsAsync(docId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Mocker at vi returner en bruker som har signert
        _signatureRepositoryMock
            .Setup(x => x.GetForDocumentVersionAsync(docId, 2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([signature]);

        // Mocker at vi henter alle brukerne. Vi har ingen målgruppe, så vi henter begge
        _userRepositoryMock
            .Setup(x => x.GetUsersByTargetAsync(
                It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([signedUser, unsignedUser]);

        // Act
        Result<IReadOnlyList<UserSignatureStatusDto>> result = await _sut.GetSignatureStatusAsync(docId);

        // Assert - Sjekker egenskapene og at en bruker har singert og den andre har ikke signert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        UserSignatureStatusDto signed = result.Value.First(u => u.UserId == signedUserId);
        signed.HasSigned.Should().BeTrue();
        signed.SignatureVersion.Should().Be(2);

        UserSignatureStatusDto unsigned = result.Value.First(u => u.UserId == unsignedUserId);
        unsigned.HasSigned.Should().BeFalse();
        unsigned.SignedAt.Should().BeNull();
    }
}