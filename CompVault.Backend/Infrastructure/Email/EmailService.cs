using CompVault.Backend.Infrastructure.Email.Config;
using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Shared.Result;
using Microsoft.Extensions.Options;
using Resend;

namespace CompVault.Backend.Infrastructure.Email;

/// <summary>
/// Service som sender epost med Resend
/// </summary>
public class EmailService(
    IOptions<EmailSettings> emailSettings,
    ILogger<EmailService> logger,
    IResend resend) : IEmailService
{

    /// <summary>
    /// Henter avsender epost fra appsettings.json. Eks: "donotreply@compvault.com"
    /// </summary>
    private readonly string _fromEmail = emailSettings.Value.FromAddress;

    /// <inheritdoc />
    public async Task<Result> SendAsync(string recipientEmail, EmailBody emailBody, CancellationToken ct = default)
    {
        try
        {
            // Vi oppretter en ResendEmailRequest-objekt som Resend krever for å sende epost.
            // Inneholder fra og til, epost tittel, html-body og text-body
            var message = new EmailMessage
            {
                From = _fromEmail,
                To = [recipientEmail],
                Subject = emailBody.Subject,
                HtmlBody = emailBody.Html,
                TextBody = null
            };

            // Sender epost med EmailSendAsync. Returnerer et response objekt
            var response = await resend.EmailSendAsync(message, ct);
            if (!response.Success)
            {
                logger.LogError("Email sending failed to {Email}. Resend response: {@Response}",
                    recipientEmail, response);
                return Result.Failure(AppError.Create(ErrorCode.EmailSendFailed, "Failed to send email"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email sending failed to {Email}", recipientEmail);
            return Result.Failure(AppError.Create(ErrorCode.EmailSendFailed, "Failed to send email"));
        }
    }
}