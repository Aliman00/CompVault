using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using CompVault.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CompVault.Tests.Backend.Features.Auth;

public class AuthServiceRefreshTokenAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AuthService _sut;

    public AuthServiceRefreshTokenAsyncTests()
    {
        // UserManager er en klasse, og ikke Interface, og krever IUserStore i konstruktøren.
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        Mock<ILogger<IAuthService>> loggerMock = new();
        _jwtServiceMock = new Mock<IJwtService>();
        Mock<IOtpCodeService> otpCodeServiceMock = new();
        Mock<IEmailService> emailServiceMock = new();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // Mocker ExecuteInTransactionAsync til å kjøre operasjonen direkte uten ekte database
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<Task<Result<RefreshTokenResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<Task<Result<RefreshTokenResponse>>>, CancellationToken>(
                (operation, _) => operation());

        var otpOptions = Options.Create(new OtpOptions
        {
            MinResponseTimeRequestOtpMs = 0,
            MinResponseTimeVerifyOtpMs = 0,
            MaxFailedAttempts = 3
        });

        _sut = new AuthService(
            _userManagerMock.Object,
            loggerMock.Object,
            _jwtServiceMock.Object,
            otpCodeServiceMock.Object,
            emailServiceMock.Object,
            otpOptions,
            _refreshTokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // -------------------------------------------------------------------------
    // Tester - Success
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester happy path — gyldig refresh token, aktiv bruker, rotasjon gjennomføres
    /// og nye tokens returneres
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokenPair()
    {
        // Arrange
        var user = TestDataSeeder.CreateApplicationUser();
        var roles = new List<string> { "Employee" };
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "old-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        const string newRefreshToken = "new-refresh-token";
        const string newAccessToken = "new-access-token";

        var request = new RefreshTokenRequest
        {
            AccessToken = "any-access-token",
            RefreshToken = storedToken.Token
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        _refreshTokenServiceMock
            .Setup(x => x.CreateRefreshTokenAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(newRefreshToken));

        _jwtServiceMock
            .Setup(x => x.GenerateAccessToken(user, roles))
            .Returns(newAccessToken);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be(newAccessToken);
        result.Value!.RefreshToken.Should().Be(newRefreshToken);

        // Verifiserer at det gamle tokenet ble revokert (token rotation)
        storedToken.IsRevoked.Should().BeTrue();

        _refreshTokenRepositoryMock.Verify(
            x => x.GetValidTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokenServiceMock.Verify(
            x => x.CreateRefreshTokenAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _jwtServiceMock.Verify(
            x => x.GenerateAccessToken(user, roles), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Tester - Failure
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at et ukjent eller utgått refresh token returnerer InvalidToken
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_TokenNotFound_ReturnsInvalidToken()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            AccessToken = "any-access-token",
            RefreshToken = "ukjent-token"
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.InvalidToken);

        // Ingen videre kall skal skje
        _userManagerMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<Task<Result<RefreshTokenResponse>>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tester at et gyldig token der tilhørende bruker ikke finnes returnerer InvalidToken
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_UserNotFound_ReturnsInvalidToken()
    {
        // Arrange
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "gyldig-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        var request = new RefreshTokenRequest
        {
            AccessToken = "any-access-token",
            RefreshToken = storedToken.Token
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(storedToken.UserId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.InvalidToken);

        _unitOfWorkMock.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<Task<Result<RefreshTokenResponse>>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tester at et gyldig token der brukeren er inaktiv returnerer InvalidToken
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_InactiveUser_ReturnsInvalidToken()
    {
        // Arrange
        var inactiveUser = TestDataSeeder.CreateApplicationUser(deletedAt: DateTime.UtcNow.AddDays(-1));
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = inactiveUser.Id,
            Token = "gyldig-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        var request = new RefreshTokenRequest
        {
            AccessToken = "any-access-token",
            RefreshToken = storedToken.Token
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(inactiveUser.Id.ToString()))
            .ReturnsAsync(inactiveUser);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.InvalidToken);

        _unitOfWorkMock.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<Task<Result<RefreshTokenResponse>>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tester at feil i CreateRefreshTokenAsync inni transaksjonen propageres korrekt
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_CreateRefreshTokenFails_ReturnsFailure()
    {
        // Arrange
        var user = TestDataSeeder.CreateApplicationUser();
        var roles = new List<string>();
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "gyldig-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        var refreshTokenError = AppError.Create(ErrorCode.InternalError, "Klarte ikke opprette refresh token");

        var request = new RefreshTokenRequest
        {
            AccessToken = "any-access-token",
            RefreshToken = storedToken.Token
        };

        _refreshTokenRepositoryMock
            .Setup(x => x.GetValidTokenAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        _refreshTokenServiceMock
            .Setup(x => x.CreateRefreshTokenAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure(refreshTokenError));

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.InternalError);

        // JwtService skal aldri kalles når refresh token-opprettelse feiler
        _jwtServiceMock.Verify(
            x => x.GenerateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IList<string>>()),
            Times.Never);
    }
}
