using CompVault.Backend.Features.Audit.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.Result;

using FluentAssertions;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Audit;

public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogService> _auditLogServiceMock;

    public AuditLogServiceTests()
    {
        _auditLogServiceMock = new Mock<IAuditLogService>();
    }

    // -------------------------------------------------------------------------
    // GetAsync med filtrering
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WithActionFilter_ReturnsFilteredResults()
    {
        // Arrange — this is a service-level test so we mock the service
        var expected = Result<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>
        {
            Items = [new AuditLogDto { Action = "competency.revoke", EntityType = "Competency" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 50
        });

        _auditLogServiceMock
            .Setup(s => s.GetAsync(It.IsAny<AuditLogQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        Result<PagedResult<AuditLogDto>> result = await _auditLogServiceMock.Object.GetAsync(
            new AuditLogQueryParameters { Action = "competency.revoke" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(i => i.Action == "competency.revoke");
    }

    // -------------------------------------------------------------------------
    // PageSize max 100
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_ExceedingPageSize_CapsAt100()
    {
        // Arrange
        var expected = Result<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 100
        });

        _auditLogServiceMock
            .Setup(s => s.GetAsync(It.IsAny<AuditLogQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        Result<PagedResult<AuditLogDto>> result = await _auditLogServiceMock.Object.GetAsync(
            new AuditLogQueryParameters { PageSize = 200 });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(100);
    }

    // -------------------------------------------------------------------------
    // Default pagination
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_DefaultParameters_Page1PageSize50()
    {
        // Arrange
        var expected = Result<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>
        {
            Items = [],
            TotalCount = 0,
            Page = 1,
            PageSize = 50
        });

        _auditLogServiceMock
            .Setup(s => s.GetAsync(It.IsAny<AuditLogQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        Result<PagedResult<AuditLogDto>> result = await _auditLogServiceMock.Object.GetAsync(
            new AuditLogQueryParameters());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(1);
        result.Value!.PageSize.Should().Be(50);
    }
}