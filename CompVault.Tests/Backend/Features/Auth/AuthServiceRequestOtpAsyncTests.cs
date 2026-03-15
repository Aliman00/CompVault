using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CompVault.Tests.Backend.Features.Auth;

public class AuthServiceRequestOtpAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IOtpCodeService> _otpCodeServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly AuthService _sut;

    public AuthServiceRequestOtpAsyncTests()
    {
        // UserManager er en klasse, og ikke Interface, og krever IUserStore i konstruktøren.
        // Vi setter alle de andre parameterne til UserManager til null da vi ikke trenger å mocke de
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mocker de andre DI-avhengighetene
        Mock<ILogger<IAuthService>> loggerMock = new Mock<ILogger<IAuthService>>();
        Mock<IJwtService> jwtServiceMock = new Mock<IJwtService>();
        _otpCodeServiceMock = new Mock<IOtpCodeService>();
        _emailServiceMock = new Mock<IEmailService>();
        
        // Oppretter configuration OtpOptions - trenger ingen delay i tester
        var otpOptions = Options.Create(new OtpOptions
        {
            MinResponseTimeRequestOtpMs = 0,
            MaxFailedAttempts = 3
        });

        _sut = new AuthService(
            _userManagerMock.Object,
            loggerMock.Object,
            jwtServiceMock.Object,
            _otpCodeServiceMock.Object,
            _emailServiceMock.Object,
            otpOptions);
    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Oppretter en ApplicationUser med påkrevde og relevante felt
    /// </summary>
    private static ApplicationUser CreateActiveUser(string email = "test@compvault.no") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        UserName = email,
        FirstName = "Fredrik",
        LastName = "Magee",
        IsActive = true,
        DeletedAt = null
    };
    
    /// <summary>
    /// Oppretter en RequestOtpRequest med samme epost som opprettet bruker og epost som DeliveryMethod
    /// </summary>
    private static RequestOtpRequest CreateRequest(string email = "test@compvault.no") => new()
    {
        Email = email,
        DeliveryMethod = OtpDeliveryMethod.Email
    };
    
    // -------------------------------------------------------------------------
    // Tester - Success
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester happy path - bruker eksisterer, vi oppretter en OtpCode og sender det som en epost.
    /// </summary>
    [Fact]
    public async Task RequestOtpAsync_ExistingUser_GeneratesOtpAndSendsEmail_ReturnsSuccess()
    {
        // Arrange
        var request = CreateRequest();
        var user = CreateActiveUser();
        const string otpCode = "476859";
        
        // mocker UserManager til å returerne opprettet bruker
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // mocker OtpCodeService til å returnere Result med Success
        _otpCodeServiceMock
            .Setup(x => x.GenerateOtpCodeAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(otpCode));
        
        // mocker EmailService til å returere Result med Success
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailBody>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        
        // Act
        var result = await _sut.RequestOtpAsync(request);
 
        // Assert - Sjekker at Result er Success, GenerateOtpCodeAsync er kalt med brukerens ID engang,
        // og SendAsync er kalt med brukerens Email en gang
        result.IsSuccess.Should().BeTrue();
        _otpCodeServiceMock.Verify(x => x.GenerateOtpCodeAsync(user.Id, 
            It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendAsync(user.Email!, It.IsAny<EmailBody>(), 
                It.IsAny<CancellationToken>()), Times.Once);
    }
    
    // -------------------------------------------------------------------------
    // Tester - Failure
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at innsendt epost ikke eksisterer i systemet. Returnerer Success med vilje selvom det er en Failure,
    /// ingen ondsinnete brukere skal vite om eposten er registrert
    /// </summary>
    [Fact]
    public async Task RequestOtpAsync_UnknownEmail_ReturnsSuccess()
    {
        // Arrange
        var request = CreateRequest();
        
        // mocker UserManager til å returnere inaktive bruker
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        
         // Act
         var result = await _sut.RequestOtpAsync(request);
         
         // Assert - Sjekker at Result er Success (selvom det er failure), og at tilhørende service-metoder blir kalt
         // 0 ganger
         result.IsSuccess.Should().BeTrue();
         _otpCodeServiceMock.Verify(x => x.GenerateOtpCodeAsync(It.IsAny<Guid>(), 
             It.IsAny<CancellationToken>()), Times.Never);
         _emailServiceMock.Verify(x => x.SendAsync(It.IsAny<string>(), 
             It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    
    /// <summary>
    /// Tester at innsendt epost tilhører en bruker som ikke er aktive.
    /// Returnerer Success med vilje selvom det er en Failure, ingen ondsinnete brukere skal vite om eposten er
    /// registrert
    /// </summary>
    [Fact]
    public async Task RequestOtpAsync_InactiveUser_ReturnsSuccess()
    {
        // Arrange
        var request = CreateRequest();
        var user = CreateActiveUser();
        user.IsActive = false;
        
        // mocker UserManager til å returnere null
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // Act
        var result = await _sut.RequestOtpAsync(request);
         
        // Assert - Sjekker at Result er Success (selvom det er failure), og at tilhørende service-metoder blir kalt
        // 0 ganger
        result.IsSuccess.Should().BeTrue();
        _otpCodeServiceMock.Verify(x => x.GenerateOtpCodeAsync(It.IsAny<Guid>(), 
            It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendAsync(It.IsAny<string>(), 
            It.IsAny<EmailBody>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    
    /// <summary>
    /// Tester at SendAsync fra EmailService failer, og returnerer Failure. Dette skal ikke skje i produksjon,
    /// derfor returnerer vi failure
    /// </summary>
    [Fact]
    public async Task RequestOtpAsync_EmailFailure_ReturnsFailure()
    {
        // Arrange
        var request = CreateRequest();
        var user = CreateActiveUser();
        const string otpCode = "476859";
        var emailError = AppError.Create(ErrorCode.EmailSendFailed, "Email service down");
        
        // mocker UserManager til å returerne opprettet bruker
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // mocker OtpCodeService til å returnere Result med Success
        _otpCodeServiceMock
            .Setup(x => x.GenerateOtpCodeAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success(otpCode));
        
        // mocker EmailService til å returere Result med Failure
        _emailServiceMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailBody>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(emailError));
        
        // Act
        var result = await _sut.RequestOtpAsync(request);
 
        // Assert - Sjekker at Result er Failure og at ErrorCode er korrekt
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.EmailSendFailed);
    }
    
    
    /// <summary>
    /// Tester at OtpService returnerer Result med Failure.
    /// Returnerer Success med vilje selvom det er en Failure, ingen ondsinnete brukere skal vite om eposten er
    /// registrert
    /// </summary>
    [Fact]
    public async Task RequestOtpAsync_GenerateOtpCodeAsyncFails_ReturnsSuccess()
    {
        // Arrange
        var request = CreateRequest();
        var user = CreateActiveUser();
        var otpCodeError = AppError.Create(ErrorCode.OtpMaxAttemptsExceeded, "Max attempts exceeded");
        
        // mocker UserManager til å returerne opprettet bruker
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        
        // mocker OtpCodeService til å returnere Result med Failure
        _otpCodeServiceMock
            .Setup(x => x.GenerateOtpCodeAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure(otpCodeError));
        
        // Act
        var result = await _sut.RequestOtpAsync(request);
 
        // Assert - Sjekker at Result er Failure og EmailService aldri blir kalt
        result.IsSuccess.Should().BeTrue();
        _emailServiceMock.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<EmailBody>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}