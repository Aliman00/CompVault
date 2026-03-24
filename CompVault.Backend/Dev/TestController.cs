using CompVault.Backend.Common.Controller;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Backend.Infrastructure.Email.Templates;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Dev;

#if DEBUG
[ApiController]
[Route("api/[controller]")]
public class TestController(IEmailService emailService) : BaseController
{
    // Brukes for å teste at OTP-email fungerer.
    [HttpPost("test-otp-email")]
    public async Task<ActionResult> TestEmailService([FromBody] TestEmailRequest request,
        CancellationToken ct = default)
    {
        EmailBody template = EmailTemplates.OtpCode("testkode");
        Result result = await emailService.SendAsync(request.RecipientEmail, template, ct);
        if (result.IsFailure)
            return HandleFailure(result);

        return Ok();
    }

    // Brukes for å teste at generell email fungerer.
    [HttpPost("test-email")]
    public async Task<ActionResult> TestEmailService([FromBody] TestEmailContentRequest request,
        CancellationToken ct = default)
    {
        EmailBody template = new(request.Subject, request.Body);
        Result result = await emailService.SendAsync(request.RecipientEmail, template, ct);
        if (result.IsFailure)
            return HandleFailure(result);

        return Ok();
    }
}
#endif
