namespace CompVault.Shared.Enums;

/// <summary>
/// Hvordan et dokument retter seg mot brukere.
/// Bestemmer hvilken target-kolonne som er relevant på <see cref="Document"/>.
/// </summary>
public enum DocumentTargetMode
{
    /// <summary>Dokumentet gjelder alle brukere, ingen målgruppe-begrensning.</summary>
    None,

    /// <summary>Dokumentet retter seg mot en spesifikk avdeling.</summary>
    Department,

    /// <summary>Dokumentet retter seg mot brukere med en spesifikk jobbtittel.</summary>
    JobTitle
}