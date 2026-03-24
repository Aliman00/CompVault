using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Dev;

#if DEBUG
public class TestEmailRequest
{
    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress(ErrorMessage = "Recipient email must be a valid email address")]
    public string RecipientEmail { get; init; } = string.Empty;
}

public class TestEmailContentRequest
{
    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress(ErrorMessage = "Recipient email must be a valid email address")]
    public string RecipientEmail { get; init; } = string.Empty;

    [Required(ErrorMessage = "Subject is required")]
    public string Subject { get; init; } = string.Empty;

    [Required(ErrorMessage = "Body is required")]
    public string Body { get; init; } = string.Empty;
}
#endif