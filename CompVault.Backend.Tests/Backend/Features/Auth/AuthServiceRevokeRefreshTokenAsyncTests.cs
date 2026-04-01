using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Backend.Tests.Common;
using CompVault.Shared.Result;

using FluentAssertions;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Auth;

public class AuthServiceRevokeRefreshTokenAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IPermissionService> _permissionServiceMock;
    private readonly AuthService _sut;

    public AuthServiceRevokeRefreshTokenAsyncTests()
    {
        // UserManager er en klasse, og ikke Interface, og krever IUserStore i konstruktøren.
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        Mock<ILogger<IAuthService>> loggerMock = new();
        Mock<IJwtService> jwtServiceMock = new();
        Mock<IOtpCodeService> otpCodeServiceMock = new();
        Mock<IEmailService> emailServiceMock = new();
        Mock<IRefreshTokenService> refreshTokenServiceMock = new();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        Mock<IUnitOfWork> unitOfWorkMock = new();
        _permissionServiceMock = new Mock<IPermissionService>();

        IOptions<OtpOptions> otpOptions = Options.Create(new OtpOptions
        {
            MinResponseTimeRequestOtpMs = 0,
            MinResponseTimeVerifyOtpMs = 0,
            MaxFailedAttempts = 3
        });

        _permissionServiceMock
            .Setup(x => x.GetPermissionsForRolesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _sut = new AuthService(
            _userManagerMock.Object,
            loggerMock.Object,
            jwtServiceMock.Object,
            otpCodeServiceMock.Object,
            emailServiceMock.Object,
            otpOptions,
            refreshTokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            unitOfWorkMock.Object,
            _permissionServiceMock.Object);
    }

    // -------------------------------------------------------------------------
    // Tester - Success
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester happy path — gyldig token som tilhører innlogget bruker blir revokert
    /// </summary>
    [Fact]
    public async Task RevokeRefreshTokenAsync_ValidToken_SetsIsRevokedAndReturnsSuccess()
    {
        // Arrange
        ApplicationUser user = TestDataFactory.CreateApplicationUser();
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "gyldig-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(storedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _refreshTokenRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result result = await _sut.RevokeRefreshTokenAsync(storedToken.Token, user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        storedToken.IsRevoked.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Tester - Failure
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at et ukjent eller utgått refresh token returnerer InvalidToken
    /// </summary>
    [Fact]
    public async Task RevokeRefreshTokenAsync_TokenNotFound_ReturnsInvalidToken()
    {
        // Arrange
        string unknownToken = "ukjent-token";

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(unknownToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        Result result = await _sut.RevokeRefreshTokenAsync(unknownToken, Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.InvalidToken);

        // SaveChanges skal aldri kalles
        _refreshTokenRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tester at et token som tilhører en annen bruker returnerer Forbidden
    /// </summary>
    [Fact]
    public async Task RevokeRefreshTokenAsync_TokenBelongsToOtherUser_ReturnsForbidden()
    {
        // Arrange
        ApplicationUser tokenOwner = TestDataFactory.CreateApplicationUser();
        var currentUserId = Guid.NewGuid(); // en annen bruker

        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = tokenOwner.Id, // tilhører tokenOwner, ikke currentUser
            Token = "annens-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(storedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        Result result = await _sut.RevokeRefreshTokenAsync(storedToken.Token, currentUserId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Forbidden);

        // Token skal ikke ha blitt revokert, og SaveChanges skal aldri kalles
        storedToken.IsRevoked.Should().BeFalse();
        _refreshTokenRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}