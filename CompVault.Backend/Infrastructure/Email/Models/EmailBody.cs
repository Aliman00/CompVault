namespace CompVault.Backend.Infrastructure.Email.Models;

/// <summary>
/// Ferdig rendret e-post klart for sending, som et record-objekt.
/// Inneholder e-post tittelen, HTML-koden og plaintext for eposter som ikke bruker HTML
/// </summary>
public sealed record EmailBody(
    string Subject,
    string Html
);
