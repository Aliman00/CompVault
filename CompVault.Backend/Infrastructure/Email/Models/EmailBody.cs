namespace CompVault.Backend.Infrastructure.Email.Models;

/// <summary>
/// Ferdig rendret e-post klart for sending, som et record-objekt.
/// Inneholder e-post tittelen og HTML-koden (ingen plaintext)
/// </summary>
public sealed record EmailBody(
    string Subject,
    string Html
);
