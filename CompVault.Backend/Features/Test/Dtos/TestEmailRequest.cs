using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Features.Test.Dtos;

/// <summary>
/// Test Request for å sende epost og sjekke at Resend fungerer
/// </summary>
public class TestEmailRequest
{
    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress(ErrorMessage = "Recipient email must be a valid email address")]
    public string RecipientEmail { get; init; } = string.Empty;
}