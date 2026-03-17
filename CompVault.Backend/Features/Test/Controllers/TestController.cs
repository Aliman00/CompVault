using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Test.Dtos;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Templates;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Test.Controllers;

#if DEBUG
[ApiController]
[Route("api/[controller]")]
public class TestController(IEmailService emailService) : BaseController
{
    /// <summary>
    /// Test endepunkt for å sjekke at epost fungerer
    /// </summary>
    /// <param name="request">TestEmailRequest med epost</param>
    /// <param name="ct"></param>
    /// <returns>Status 200 Ok</returns>
    [HttpPost("test-email")]
    public async Task<ActionResult> TestEmailService([FromBody] TestEmailRequest request,
        CancellationToken ct = default)
    {
        var template = EmailTemplates.OtpCode("testkode");
        var result = await emailService.SendAsync(request.RecipientEmail, template, ct);
        if (result.IsFailure)
            return HandleFailure(result);

        return Ok();
    }
}
#endif
