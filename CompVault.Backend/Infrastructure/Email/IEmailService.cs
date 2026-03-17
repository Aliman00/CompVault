using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Shared.Result;

namespace CompVault.Backend.Infrastructure.Email;

public interface IEmailService
{
    /// <summary>
    /// Sender en e-post med ferdig rendret innhold via Resend. Innholdet bygges i EmailTemplate
    /// og denne metoden utfører sending
    /// </summary>
    /// <param name="recipientEmail">Mottaker eposten som skal få meldingen</param>
    /// <param name="emailBody">Ferdig template</param>
    /// <param name="ct"></param>
    /// <returns>Result med Success hvis mail sendt eller Failure hvis noe gikk galt</returns>
    Task<Result> SendAsync(string recipientEmail, EmailBody emailBody, CancellationToken ct);
}
