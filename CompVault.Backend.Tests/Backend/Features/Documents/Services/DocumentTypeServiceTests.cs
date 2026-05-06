using CompVault.Backend.Domain.Entities.Documents;
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

public class DocumentTypeServiceTests
{
    private readonly Mock<IDocumentTypeRepository> _documentTypeRepositoryMock;
    private readonly Mock<IDocumentTypeCategoryRepository> _categoryRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly DocumentTypeService _sut;

    public DocumentTypeServiceTests()
    {
        _documentTypeRepositoryMock = new Mock<IDocumentTypeRepository>();
        _categoryRepositoryMock = new Mock<IDocumentTypeCategoryRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        var loggerMock = new Mock<ILogger<DocumentTypeService>>();

        _sut = new DocumentTypeService(
            _documentTypeRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            loggerMock.Object,
            _userRepositoryMock.Object,
            _documentRepositoryMock.Object);
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetAllAsync returnerer tom liste når ingen dokumenttyper finnes.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_NoTypes_ReturnsEmptyList()
    {
        // Arrange
        _documentTypeRepositoryMock
            .Setup(x => x.GetAllWithCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        Result<IReadOnlyList<DocumentTypeDto>> result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    /// <summary>
    /// Tester at GetAllAsync mapper dokumenttyper korrekt.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_HasTypes_ReturnsMappedDtos()
    {
        // Arrange
        var types = new List<DocumentType>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "HMS", Slug = "hms",
                TargetMode = DocumentTargetMode.Department, IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Kurs", Slug = "kurs",
                TargetMode = DocumentTargetMode.None, IsActive = true
            }
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetAllWithCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(types);

        // Act
        Result<IReadOnlyList<DocumentTypeDto>> result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("HMS");
        result.Value[1].Slug.Should().Be("kurs");
    }

    // -------------------------------------------------------------------------
    // GetBySlugAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetBySlugAsync returnerer failure når slug ikke finnes.
    /// </summary>
    [Fact]
    public async Task GetBySlugAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<DocumentTypeDto> result = await _sut.GetBySlugAsync("nonexistent");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at GetBySlugAsync returnerer korrekt DTO.
    /// </summary>
    [Fact]
    public async Task GetBySlugAsync_Found_ReturnsDto()
    {
        // Arrange
        var documentType = new DocumentType
        {
            Id = Guid.NewGuid(),
            Name = "HMS",
            Slug = "hms",
            IsActive = true
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesBySlugAsync("hms", It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        // Act
        Result<DocumentTypeDto> result = await _sut.GetBySlugAsync("hms");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Slug.Should().Be("hms");
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at CreateAsync returnerer failure når slug allerede er i bruk.
    /// </summary>
    [Fact]
    public async Task CreateAsync_SlugExists_ReturnsConflictFailure()
    {
        // Arrange
        var request = new CreateDocumentTypeRequest
        {
            Name = "HMS",
            TargetMode = DocumentTargetMode.Department
        };

        _documentTypeRepositoryMock
            .Setup(x => x.SlugExistsAsync("hms", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<DocumentTypeDto> result = await _sut.CreateAsync(request, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
    }

    /// <summary>
    /// Tester at CreateAsync oppretter dokumenttypen med korrekte verdier.
    /// </summary>
    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var createdById = Guid.NewGuid();
        var request = new CreateDocumentTypeRequest
        {
            Name = "HMS Dokumenter",
            Description = "Helse, miljø og sikkerhet",
            TargetMode = DocumentTargetMode.Department
        };

        // Slug auto-genereres fra navn: "HMS Dokumenter" → "hms-dokumenter"
        _documentTypeRepositoryMock
            .Setup(x => x.SlugExistsAsync("hms-dokumenter", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _documentTypeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType dt, CancellationToken _) => dt);

        _documentTypeRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                new DocumentType
                {
                    Id = id,
                    Name = request.Name,
                    Slug = "hms-dokumenter",
                    Description = request.Description,
                    TargetMode = request.TargetMode,
                    IsActive = true
                });

        // Act
        Result<DocumentTypeDto> result = await _sut.CreateAsync(request, createdById);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Name.Should().Be("HMS Dokumenter");
        result.Value?.Slug.Should().Be("hms-dokumenter");

        _documentTypeRepositoryMock.Verify(
            x => x.AddAsync(It.Is<DocumentType>(dt =>
                dt.Name == request.Name &&
                dt.Slug == "hms-dokumenter" &&
                dt.StorageFolder == "hms-dokumenter" &&
                dt.CreatedById == createdById), It.IsAny<CancellationToken>()),
            Times.Once);

        _documentTypeRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UpdateAsync returnerer failure når dokumenttypen ikke finnes.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        string slug = "nonexistent";
        var request = new UpdateDocumentTypeRequest { Name = "Nytt navn" };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<DocumentTypeDto> result = await _sut.UpdateAsync(slug, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at UpdateAsync oppdaterer kun angitte felt.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        string slug = "gammel-slug";
        var existing = new DocumentType
        {
            Id = Guid.NewGuid(),
            Name = "Gammelt navn",
            Slug = slug,
            Description = "Gammel beskrivelse",
            TargetMode = DocumentTargetMode.None,
            IsActive = true
        };

        var request = new UpdateDocumentTypeRequest
        {
            Name = "Nytt navn",
            // Description og TargetMode er null — skal ikke endres
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _documentTypeRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentTypeRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentTypeRepositoryMock
            .Setup(x => x.GetWithCategoriesAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        Result<DocumentTypeDto> result = await _sut.UpdateAsync(slug, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existing.Name.Should().Be("Nytt navn");
        existing.Description.Should().Be("Gammel beskrivelse");
        existing.TargetMode.Should().Be(DocumentTargetMode.None);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at DeleteAsync returnerer failure når dokumenttypen ikke finnes.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        string slug = "nonexistent";
        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<bool> result = await _sut.DeleteAsync(slug);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at DeleteAsync soft-sletter ved å sette IsActive = false.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_Found_SetsInactiveAndReturnsTrue()
    {
        // Arrange
        string slug = "hms-documents";
        var documentType = new DocumentType { Id = Guid.NewGuid(), Name = "HMS", Slug = slug, IsActive = true };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _documentTypeRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DocumentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _documentTypeRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<bool> result = await _sut.DeleteAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        documentType.IsActive.Should().BeFalse();
        documentType.DeletedAt.Should().NotBeNull();
    }

    // -------------------------------------------------------------------------
    // GetCategoriesAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at GetCategoriesAsync returnerer failure når dokumenttypen ikke finnes.
    /// </summary>
    [Fact]
    public async Task GetCategoriesAsync_DocumentTypeNotFound_ReturnsFailure()
    {
        // Arrange
        string slug = "nonexistent";
        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<IReadOnlyList<DocumentTypeCategoryDto>> result =
            await _sut.GetCategoriesAsync(slug);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at GetCategoriesAsync returnerer kategorier for en dokumenttype.
    /// </summary>
    [Fact]
    public async Task GetCategoriesAsync_Found_ReturnsMappedCategories()
    {
        // Arrange
        var typeId = Guid.NewGuid();
        string slug = "hms-documents";
        var documentType = new DocumentType { Id = typeId, Slug = slug, IsActive = true };

        var categories = new List<DocumentTypeCategory>
        {
            new()
            {
                Id = Guid.NewGuid(), DocumentTypeId = typeId,
                Name = "Nødsprosedyrer", Slug = "nodprosedyrer"
            },
            new()
            {
                Id = Guid.NewGuid(), DocumentTypeId = typeId,
                Name = "Sikkerhet", Slug = "sikkerhet"
            }
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _categoryRepositoryMock
            .Setup(x => x.GetByDocumentTypeIdAsync(typeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        Result<IReadOnlyList<DocumentTypeCategoryDto>> result =
            await _sut.GetCategoriesAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("Nødsprosedyrer");
    }

    // -------------------------------------------------------------------------
    // CreateCategoryAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at CreateCategoryAsync returnerer failure når dokumenttypen ikke finnes.
    /// </summary>
    [Fact]
    public async Task CreateCategoryAsync_DocumentTypeNotFound_ReturnsFailure()
    {
        // Arrange
        string typeSlug = "nonexistent";
        var request = new CreateDocumentTypeCategoryRequest { Name = "Test" };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(typeSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentType?)null);

        // Act
        Result<DocumentTypeCategoryDto> result =
            await _sut.CreateCategoryAsync(typeSlug, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at CreateCategoryAsync returnerer conflict når slug allerede finnes.
    /// </summary>
    [Fact]
    public async Task CreateCategoryAsync_SlugExists_ReturnsConflictFailure()
    {
        // Arrange
        var typeId = Guid.NewGuid();
        string slug = "hms-documents";
        var documentType = new DocumentType { Id = typeId, Slug = slug, IsActive = true };
        var request = new CreateDocumentTypeCategoryRequest { Name = "Test" };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        // Slug auto-genereres fra navn: "Test" → "test"
        _categoryRepositoryMock
            .Setup(x => x.SlugExistsAsync(typeId, "test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<DocumentTypeCategoryDto> result =
            await _sut.CreateCategoryAsync(slug, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Conflict);
    }

    /// <summary>
    /// Tester happy path for CreateCategoryAsync.
    /// </summary>
    [Fact]
    public async Task CreateCategoryAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var typeId = Guid.NewGuid();
        string slug = "hms-documents";
        var documentType = new DocumentType { Id = typeId, Slug = slug, IsActive = true };
        var request = new CreateDocumentTypeCategoryRequest
        {
            Name = "Nødsprosedyrer"
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        // Slug auto-genereres: "Nødsprosedyrer" → "noedsprosedyrer"
        _categoryRepositoryMock
            .Setup(x => x.SlugExistsAsync(typeId, "noedsprosedyrer", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<DocumentTypeCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTypeCategory c, CancellationToken _) => c);

        _categoryRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<DocumentTypeCategoryDto> result =
            await _sut.CreateCategoryAsync(slug, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Name.Should().Be("Nødsprosedyrer");
        result.Value?.Slug.Should().Be("noedsprosedyrer");

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.Is<DocumentTypeCategory>(c =>
                c.DocumentTypeId == typeId &&
                c.Name == "Nødsprosedyrer"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateCategoryAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at UpdateCategoryAsync returnerer failure når kategorien ikke finnes
    /// eller tilhører en annen dokumenttype.
    /// </summary>
    [Fact]
    public async Task UpdateCategoryAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        var typeId = Guid.NewGuid();
        string typeSlug = "hms-documents";
        var categoryId = Guid.NewGuid();
        var documentType = new DocumentType { Id = typeId, Slug = typeSlug, IsActive = true };
        var request = new UpdateDocumentTypeCategoryRequest { Name = "Test" };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(typeSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTypeCategory?)null);

        // Act
        Result<DocumentTypeCategoryDto> result =
            await _sut.UpdateCategoryAsync(typeSlug, categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at UpdateCategoryAsync returnerer failure når kategorien tilhører
    /// en annen dokumenttype.
    /// </summary>
    [Fact]
    public async Task UpdateCategoryAsync_WrongDocumentType_ReturnsFailure()
    {
        // Arrange
        var typeId = Guid.NewGuid();
        string typeSlug = "hms-documents";
        var otherTypeId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var documentType = new DocumentType { Id = typeId, Slug = typeSlug, IsActive = true };

        var category = new DocumentTypeCategory
        {
            Id = categoryId,
            DocumentTypeId = otherTypeId,
            Name = "Test",
            Slug = "test"
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(typeSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var request = new UpdateDocumentTypeCategoryRequest { Name = "Ny" };

        // Act
        Result<DocumentTypeCategoryDto> result =
            await _sut.UpdateCategoryAsync(typeSlug, categoryId, request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester happy path for UpdateCategoryAsync.
    /// </summary>
    [Fact]
    public async Task UpdateCategoryAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var typeId = Guid.NewGuid();
        string typeSlug = "hms-documents";
        var categoryId = Guid.NewGuid();
        var documentType = new DocumentType { Id = typeId, Slug = typeSlug, IsActive = true };
        var category = new DocumentTypeCategory
        {
            Id = categoryId,
            DocumentTypeId = typeId,
            Name = "Gammel",
            Slug = "gammel"
        };

        var request = new UpdateDocumentTypeCategoryRequest { Name = "Ny" };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(typeSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // Slug auto-genereres fra navn: "Ny" → "ny"
        _categoryRepositoryMock
            .Setup(x => x.SlugExistsAsync(typeId, "ny", categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DocumentTypeCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _categoryRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<DocumentTypeCategoryDto> result =
            await _sut.UpdateCategoryAsync(typeSlug, categoryId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Name.Should().Be("Ny");
        category.Name.Should().Be("Ny");
        category.Slug.Should().Be("ny");
    }

    // -------------------------------------------------------------------------
    // DeleteCategoryAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at DeleteCategoryAsync returnerer failure når kategorien ikke finnes.
    /// </summary>
    [Fact]
    public async Task DeleteCategoryAsync_CategoryNotFound_ReturnsFailure()
    {
        // Arrange
        string typeSlug = "hms-documents";
        var categoryId = Guid.NewGuid();
        var documentType = new DocumentType { Id = Guid.NewGuid(), Slug = typeSlug, Name = "HMS Documents", IsActive = true };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(typeSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentTypeCategory?)null);

        // Act
        Result<bool> result = await _sut.DeleteCategoryAsync(typeSlug, categoryId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    /// <summary>
    /// Tester at DeleteCategoryAsync soft-sletter kategorien.
    /// </summary>
    [Fact]
    public async Task DeleteCategoryAsync_ValidRequest_SetsInactive()
    {
        // Arrange
        var typeId = Guid.NewGuid();
        string typeSlug = "hms-documents";
        var categoryId = Guid.NewGuid();
        var documentType = new DocumentType { Id = typeId, Slug = typeSlug, Name = "HMS Documents", IsActive = true };
        var category = new DocumentTypeCategory
        {
            Id = categoryId,
            DocumentTypeId = typeId,
            Name = "Test",
            IsActive = true
        };

        _documentTypeRepositoryMock
            .Setup(x => x.GetBySlugAsync(typeSlug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentType);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        _categoryRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<DocumentTypeCategory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _categoryRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<bool> result = await _sut.DeleteCategoryAsync(typeSlug, categoryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.IsActive.Should().BeFalse();
    }
}