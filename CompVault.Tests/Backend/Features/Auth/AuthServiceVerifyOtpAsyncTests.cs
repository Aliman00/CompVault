using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using CompVault.Tests.Common;
using CompVault.Tests.Common.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CompVault.Tests.Backend.Features.Auth;

public class AuthServiceVerifyOtpAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IOtpCodeService> _otpCodeServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly AuthService _sut;

    public AuthServiceVerifyOtpAsyncTests()
    {
        // UserManager er en klasse, og ikke Interface, og krever IUserStore i konstruktøren.
        // Vi setter alle de andre parameterne til UserManager til null da vi ikke trenger å mocke de
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mocker de andre DI-avhengighetene
        Mock<ILogger<IAuthService>> loggerMock = new Mock<ILogger<IAuthService>>();
        _otpCodeServiceMock = new Mock<IOtpCodeService>();
        Mock<IEmailService> emailServiceMock = new Mock<IEmailService>();
        _jwtServiceMock = new Mock<IJwtService>();
        
        // Oppretter configuration OtpOptions - trenger ingen delay i tester
        var otpOptions = Options.Create(new OtpOptions
        {
            MinResponseTimeRequestOtpMs = 0,
            MaxFailedAttempts = 3
        });

        _sut = new AuthService(
            _userManagerMock.Object,
            loggerMock.Object,
            _jwtServiceMock.Object,
            _otpCodeServiceMock.Object,
            emailServiceMock.Object,
            otpOptions);
    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Oppretter en VerifyOtpRequest med samme epost som brukeren og en tilfeldig kode
    /// </summary>
    private static VerifyOtpRequest CreateRequest(string email = TestConstants.Users.DefaultEmailForActiveUser, 
        string otpCode = TestConstants.Otp.PlainTextOtpCode) => new()
    {
        Email = email,
        OtpCode = otpCode
    };
    
    // -------------------------------------------------------------------------
    // Tester - Success
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester happy path - bruker eksisterer, koden blir verifisert som korrekt, Access og Refresh-token blir
    /// opprettet og vi returnerer LoginResponse
    /// </summary>
    [Fact]
    public async Task VerifyOtpAsync_ExistingUserAndCorrectCode_ReturnsLoginResponse()
    {
        // Arrange
        var request = CreateRequest();
        var user = TestDataSeeder.CreateApplicationUser();
        var roles = new List<string>();
        const string accessToken = "access-token";
        const string refreshToken = "refresh-token";
        
        
        // mocker UserManager til å returerne opprettet bruker
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // mocker OtpCodeService til å returnere Result med Success
        _otpCodeServiceMock
            .Setup(x => x.VerifyOtpCodeAsync(user.Id, request.OtpCode, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        
        // mocker UserManager til å returnere roller til brukerne
        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);
        
        // mocker JwtService til å returere AccessToken
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(user, roles))
            .Returns(accessToken);
        
        // mocker JwtService til å returere RefreshToken
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns(refreshToken);
        
        // Act
        var result = await _sut.VerifyOtpAsync(request);
 
        // Assert - Sjekker at Result er Success og at LoginResponse inneholder korrekte verdier
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be(accessToken);
        result.Value!.RefreshToken.Should().Be(refreshToken);
        result.Value!.UserId.Should().Be(user.Id);
        result.Value!.FullName.Should().Be($"{user.FirstName} {user.LastName}");
        result.Value!.Roles.Should().BeEquivalentTo(roles);
        
        // Verfiserer at alle servicene ble kalt engang
        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once);
        _otpCodeServiceMock.Verify(x => x.VerifyOtpCodeAsync(user.Id, request.OtpCode,
            It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Once);
        _jwtServiceMock.Verify(x => x.GenerateAccessToken(user, roles), Times.Once());
        _jwtServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Once());
    }
    
    // -------------------------------------------------------------------------
    // Tester - Failure
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at en emailen fra Requesten ikke tilhører en bruker og kallet til FindByEmailAsync
    /// returnerer null
    /// </summary>
    [Fact]
    public async Task VerifyOtpAsync_UnknownEmail_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest();
        
        // mocker UserManager til å returerne null
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        
        // Act
        var result = await _sut.VerifyOtpAsync(request);
 
        // Assert - Sjekker at Result er Failure og at error-koden er OtpInvalidOrExpired
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.OtpInvalidOrExpired);
        
        // Verfiserer at kun FindByEmailAsync blir kalt, og OtpCodeService ikke blir kalt
        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once);
        _otpCodeServiceMock.Verify(x => x.VerifyOtpCodeAsync(It.IsAny<Guid>(), request.OtpCode,
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    /// <summary>
    /// Tester at OtpCodeService returnerer Failure og at vi videresender AppError til
    /// kalleren og at ingen flere metoder blir kalt
    /// </summary>
    [Fact]
    public async Task VerifyOtpAsync_OtpCodeServiceFails_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest();
        var user = TestDataSeeder.CreateApplicationUser();
        var otpCodeError = AppError.Create(ErrorCode.OtpMaxAttemptsExceeded, 
            "Too many failed attempts");
        
        // mocker UserManager til å returerne opprettet bruker
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // mocker OtpCodeService til å returnere Result med Failure
        _otpCodeServiceMock
            .Setup(x => x.VerifyOtpCodeAsync(user.Id, request.OtpCode, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(otpCodeError));
        
        // Act
        var result = await _sut.VerifyOtpAsync(request);
 
        // Assert - Sjekker at Result er Failure og at error-koden er OtpMaxAttemptsExceeded
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.OtpMaxAttemptsExceeded);
        
        // Verifiserer at FindByEmailAsync og VerifyOtpCodeAsync blir kalt, men ikke GetRolesAsync
        _userManagerMock.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Once);
        _otpCodeServiceMock.Verify(x => x.VerifyOtpCodeAsync(It.IsAny<Guid>(), request.OtpCode,
            It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.GetRolesAsync(user), Times.Never);
    }
    
}