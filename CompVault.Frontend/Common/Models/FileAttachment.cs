namespace CompVault.Frontend.Common.Models;

/// <summary>
/// Record for å sende data til en fil mellom lag i frontend, fær det sendes til backend. Eks: komponent -> service
/// </summary>
/// <param name="Stream">Stream-en til filen</param>
/// <param name="FileName">Navnet på filen fra brukerens maskin</param>
/// <param name="ContentType">Type-innehold som en string. Eks: applicaiton/pfd, image/jpeg </param>
public record FileAttachment(
    Stream Stream,
    string FileName,
    string ContentType);