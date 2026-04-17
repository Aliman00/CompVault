namespace CompVault.Shared.Enums;

/// <summary>
/// Hvordan et dokument retter seg mot brukere.
/// Bestemmer hvilken target-kolleksjon som er relevant på Document.
/// </summary>
public enum DocumentTargetMode
{
    /// <summary>Dokumentet gjelder alle brukere, ingen målgruppe-begrensning.</summary>
    None,

    /// <summary>Dokumentet retter seg mot én eller flere avdelinger (via DocumentDepartment-koblinger).</summary>
    Department,

    /// <summary>Dokumentet retter seg mot brukere med én eller flere jobbtitler (via DocumentJobTitle-koblinger).</summary>
    JobTitle
}