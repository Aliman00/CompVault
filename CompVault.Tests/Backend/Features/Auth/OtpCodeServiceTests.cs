using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Features.Auth;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Shared.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CompVault.Tests.Backend.Features.Auth;

public class OtpCodeServiceTests
{
    private readonly Mock<IOtpCodeRepository> _otpCodeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly OtpCodeService _sut;
    
    public OtpCodeServiceTests()
    {
        // Mocker DI-avhengighetene
        Mock<ILogger<OtpCodeService>> loggerMock = new Mock<ILogger<OtpCodeService>>();
        _otpCodeRepositoryMock = new Mock<IOtpCodeRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        
        // Oppretter configuration OtpOptions - trenger ingen delay i tester
        var otpOptions = Options.Create(new OtpOptions
        {
            MinResponseTimeRequestOtpMs = 0,
            MaxFailedAttempts = 3
        });

        _sut = new OtpCodeService(
            loggerMock.Object,
            otpOptions,
            _otpCodeRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    
    // -------------------------------------------------------------------------
    // GenerateOtpCodeAsync - Success test
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester happy path. Sjekker at ingen Otp-kode allerede eksisterer, at koden lagres korrekt og at
    /// vi returnerer Result med Success med ritkige egenskaper. Sjekker at koden er korrekt lengde og at
    /// metodene blir kalt hvertfall en gang
    /// </summary>
    [Fact]
    public async Task GenerateOtpCodeAsync_NoExistingCode_ReturnsSuccess()
    {
        // Arrange - Setter opp en brukerId og variabelen for å hente den lagrede Otp-koden
        var userId = Guid.NewGuid();
        
        // mocker at GetActiveCodeAsync returner null - ingen eksisterende OtpCode for brukeren
        _otpCodeRepositoryMock
            .Setup(x => x.GetActiveCodeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OtpCode?)null);
        
        // Act
        var result = await _sut.GenerateOtpCodeAsync(userId);
        
        // Assert - Sjekker at Result er Success, og at metodene blir kalt en gang
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveLength(6);
        result.Value.Should().MatchRegex(@"^\d{6}$");
        _otpCodeRepositoryMock.Verify(x => x.GetActiveCodeAsync(userId, 
            It.IsAny<CancellationToken>()), Times.Once);
        _otpCodeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OtpCode>(), 
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }
    
    /// <summary>
    /// Tester happy path. Sjekker at ingen OtpKoden som blir lagret er korrekt med riktige egenskaper og at hashen
    /// har fungert
    /// </summary>
    [Fact]
    public async Task GenerateOtpCodeAsync_NoExistingCode_SavesOtpCode()
    {
        // Arrange - Setter opp en brukerId og variabelen for å hente den lagrede Otp-koden
        var userId = Guid.NewGuid();
        OtpCode? capturedOtpCode = null;
        
        // mocker at GetActiveCodeAsync returner null - ingen eksisterende OtpCode for brukeren
        _otpCodeRepositoryMock
            .Setup(x => x.GetActiveCodeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OtpCode?)null);
            
        // mocker AddAsync og returnerer OtpCode-objektet som blir lagt til
        _otpCodeRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<OtpCode>(), It.IsAny<CancellationToken>()))
            .Callback<OtpCode, CancellationToken>((otp, _) => capturedOtpCode = otp);
        
        // Act
        var result = await _sut.GenerateOtpCodeAsync(userId);
        
        // Assert - Sjekker at objektet ikke er null, at hashen har fungert,og at det har riktig verdier
        capturedOtpCode.Should().NotBeNull();
        capturedOtpCode.Code.Should().HaveLength(64); // Code skal være 64 hvis hash fungerer
        // Sjekker at otp-koden metoden lagrer ikke er lik som koden som returnes. 
        capturedOtpCode.Code.Should().NotBe(result.Value); 
        capturedOtpCode.UserId.Should().Be(userId); // Sjekker at userId er riktig userId
    }
    
    
    
    // -------------------------------------------------------------------------
    // GenerateOtpCodeAsync - Failure tester
    // -------------------------------------------------------------------------'
    /// <summary>
    /// Tester at det eksisterer en aktive Otp-kode på brukeren. Da kan ikke brukeren få en ny, når den er på cooldown
    /// Returner Result med Failure
    /// </summary>
    [Fact]
    public async Task GenerateOtpCodeAsync_ActiveOtpExists_ReturnsOtpCooldownError()
    {
        // Arrange - Setter opp en brukerId og variabelen for å hente den lagrede Otp-koden
        var userId = Guid.NewGuid();
        var otpCode = new OtpCode
        {
            UserId = userId,
            Code = "secrethashedcode",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        
        // mocker at GetActiveCodeAsync returner eksisterende Otp-kode
        _otpCodeRepositoryMock
            .Setup(x => x.GetActiveCodeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otpCode);
        
        // Act
        var result = await _sut.GenerateOtpCodeAsync(userId);
        
        // Assert - Sjekker at objektet ikke er null, at hashen har fungert,og at det har riktig verdier.
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.OtpCooldown);
        // Sjekker at ingen flere metoder blir kalt
        _otpCodeRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OtpCode>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }
    
    /// <summary>
    /// Mocker en Race Condition hvor to kall treffer nesten samtidig. Vi har lagt inn en kode i databasen, som sikrer
    /// at kun en av kallene får en kode.
    /// </summary>
    [Fact]
    public async Task GenerateOtpCodeAsync_WhenRaceConditionOccurs_ReturnsOtpCooldownError()
    {
        // Arrange - Setter opp en brukerId
        var userId = Guid.NewGuid();
        
        // mocker at GetActiveCodeAsync returner null - ingen eksisterende OtpCode for brukeren
        _otpCodeRepositoryMock
            .Setup(x => x.GetActiveCodeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OtpCode?)null);
        
        // mocker at unitOfWorkMock kaster DbUpdateException()
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        
        // Act
        var result = await _sut.GenerateOtpCodeAsync(userId);
        
        // Assert - Sjekker at Result er Failure og at vi får OtpCooldown som Error
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.OtpCooldown);
    }
    
    
}