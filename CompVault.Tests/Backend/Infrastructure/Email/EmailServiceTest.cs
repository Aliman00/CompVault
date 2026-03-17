using System.Net;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Config;
using CompVault.Backend.Infrastructure.Email.Templates;
using CompVault.Shared.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Resend;

namespace CompVault.Tests.Backend.Infrastructure.Email;

public class EmailServiceTest
{
    // Mocker servicer EmailService kaller
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Mock<IResend> _resendMock;

    // Systemet vi tester
    private readonly EmailService _sut;

    // Mottaker og avsender epost for testing
    private const string FromEmail = "donotreply@compvault.com";
    private const string RecipientEmail = "test@example.com";

    // Mocker config fra AppSettings
    private static readonly EmailSettings EmailSettings = new()
    {
        ApiKey = "test-api-key",
        FromAddress = FromEmail
    };

    // Oppretter en konstruktør for å kunne teste EmailService med mockede servicer
    public EmailServiceTest()
    {
        _loggerMock = new Mock<ILogger<EmailService>>();
        _resendMock = new Mock<IResend>();

        _sut = new EmailService(Options.Create(EmailSettings), _loggerMock.Object, _resendMock.Object);
    }

    /// <summary>
    /// Tester en suksessful epost sending- Resend gir oss ResendResponse med Success og
    /// SendAsync returnerer success
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenResendSucceeds_ReturnsSuccess()
    {
        // Arrange
        var emailBody = EmailTemplates.OtpCode("TestCode");

        // Mocker en success melding fra ResendResponse-objektet
        _resendMock
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), null));

        // Act
        var result = await _sut.SendAsync(RecipientEmail, emailBody);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// Tester når responsen fra resend sin EmailSendAsync gir feilmelding og vi
    /// går inn i (!response.Success)-kodeblokken
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenResendFails_ReturnsFailure()
    {
        // Arrange
        var emailBody = EmailTemplates.OtpCode("TestCode");

        // Mocker en failure melding fra ResendResponse-objektet
        _resendMock
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendResponse<Guid>(new ResendException(HttpStatusCode.InternalServerError,
                        ErrorType.ApplicationError, "Application Error"), null));

        // Act
        var result = await _sut.SendAsync(RecipientEmail, emailBody);

        // Assert - Sjekker at IsFailure er true og at ErrorCode er EmailSendFailed
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.EmailSendFailed, result.Error!.Code);
    }

    /// <summary>
    /// Tester når resend sin EmailSendAsync kaster en exception som vi fanger opp i try-catch
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenResendThrowsException_ReturnsFailure()
    {
        // Arrange
        var emailBody = EmailTemplates.OtpCode("TestCode");
        var exception = new Exception("Resend is down for maintenance");

        // Mocker en failure melding fra ResendResponse-objektet
        _resendMock
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _sut.SendAsync(RecipientEmail, emailBody);

        // Assert - Sjekker at IsFailure er true og at ErrorCode er EmailSendFailed
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.EmailSendFailed, result.Error!.Code);
    }

    /// <summary>
    /// Tester logging når resend sin EmailSendAsync kaster en exception som vi fanger opp i try-catch.
    /// Viktig med en logging test for å verifisere at vi klarer å fange opp hvis en epost ikke blir sendt
    /// </summary>
    [Fact]
    public async Task SendAsync_WhenResendThrowsException_LogsError()
    {
        // Arrange
        var emailBody = EmailTemplates.OtpCode("TestCode");
        var exception = new Exception("Resend is down for maintenance");

        // Mocker en failure melding fra ResendResponse-objektet
        _resendMock
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _sut.SendAsync(RecipientEmail, emailBody);

        // Assert - Sjekker at vi logger riktig error, exception og at den inneholder recipientEmail
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(RecipientEmail)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tester at EmailService bygger EmailMessage-objektet korrekt og at alle egenskapene
    /// stemmer. Fanger opp objektet servicen lager med Callback
    /// </summary>
    [Fact]
    public async Task SendAsync_SendsCorrectEmailMessage()
    {
        // Arrange
        var code = "244309";
        var emailBody = EmailTemplates.OtpCode(code);

        EmailMessage? capturedMessage = null;

        // Mocker en success melding fra ResendResponse-objektet og henter EmailMessage etter servicen
        // har opprettet den
        _resendMock
            .Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _)
                => capturedMessage = msg)
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), null));

        // Act
        await _sut.SendAsync(RecipientEmail, emailBody);

        // Assert - Sjekker at alle egenskapene stemmer mot det vi har gitt metoden
        Assert.NotNull(capturedMessage);
        Assert.Equal(FromEmail, capturedMessage.From);
        Assert.Contains(RecipientEmail, capturedMessage.To);
        Assert.Equal($"Din engangskode: {code}", capturedMessage.Subject);
        Assert.Equal($"<p>Din engangskode er: <strong>{code}</strong></p>", capturedMessage.HtmlBody);
    }
}
