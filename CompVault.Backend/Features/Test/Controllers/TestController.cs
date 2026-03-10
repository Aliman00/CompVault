using CompVault.Backend.Controllers;
using CompVault.Backend.Features.Test.Dtos;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Templates;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController(IEmailService emailService) : BaseController
{
    /// <summary>
    /// Test endepunkt for å sjekke at epost fungerer
    /// </summary>
    /// <param name="request">TestEmailRequest med epost</param>
    /// <returns>Status 200 Ok</returns>
    [HttpPost("test-email")]
    public async Task<ActionResult> TestEmailService([FromBody] TestEmailRequest request)
    {
        var template = EmailTemplates.SimpleText("Test tittel", "Test body");
        var result = await emailService.SendAsync(request.RecipientEmail, template);
        if (result.IsFailure)
            return HandleFailure(result);

        return Ok();
    }
}