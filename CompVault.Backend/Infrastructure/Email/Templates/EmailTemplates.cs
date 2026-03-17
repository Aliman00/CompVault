using CompVault.Backend.Infrastructure.Email.Models;
namespace CompVault.Backend.Infrastructure.Email.Templates;

/// <summary>
/// Generisk epost template som oppretter en epost med tittel (subject) og epost body med HTML-kode
/// </summary>
public static class EmailTemplates
{
    public static EmailBody SimpleText(string subject, string message) => new(
        Subject: subject,
        Html: $"<p>{message}</p>"
    );
}