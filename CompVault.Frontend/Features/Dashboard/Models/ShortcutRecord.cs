namespace CompVault.Frontend.Features.Dashboard.Models;

/// <summary>
/// Record for å enkelt kunne bytte mellom snarveier og bytte farger/tekst
/// </summary>
/// <param name="Label">Teksten under ikonet</param>
/// <param name="Icon">Hvilket ikon til hver snarvei</param>
/// <param name="Url">URL-en for navigienrg</param>
/// <param name="AccentColor">Fargen på ikonet</param>
/// <param name="IconBackgroundColor">Bakgrunnsfargen</param>
public record Shortcut(
    string Label,
    string Icon,
    string Url,
    string AccentColor,
    string IconBackgroundColor);