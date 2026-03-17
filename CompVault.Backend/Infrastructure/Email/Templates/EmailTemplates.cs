using CompVault.Backend.Infrastructure.Email.Models;
namespace CompVault.Backend.Infrastructure.Email.Templates;

/// <summary>
/// Generisk epost template som oppretter en epost med tittel (subject) og epost body med HTML-kode
/// </summary>
public static class EmailTemplates
{
    /// <summary>
    /// Brukes til å sende OtpCode. Koden i subject og body, for best bruker opplevelse.
    /// Viktig at code er i Subject etter : for testing
    /// </summary>
    /// <param name="code">Otp-kode</param>
    /// <returns>Ferdig bygget EmailBody</returns>
    public static EmailBody OtpCode(string code) => new(
        Subject: $"Din engangskode: {code}",
        Html: $"<p>Din engangskode er: <strong>{code}</strong></p>"
    );
}
