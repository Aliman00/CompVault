using CompVault.Shared.Enums;
namespace CompVault.Frontend.Common.Extensions;

/// <summary>
/// Metoder for visning av enum i UI
/// </summary>
public static class EnumDisplayExtensions
{   
    /// <summary>
    /// Oversetter fra engelsk til norsk
    /// </summary>
    public static string ToDisplayString(this EmploymentType type) => type switch
    {
        EmploymentType.Permanent => "Fast",
        EmploymentType.Temporary => "Midlertidig",
        EmploymentType.Contracted => "Innleid",
        _ => type.ToString()
    };
    
    /// <summary>
    /// Oversetter fra engelsk til norsk
    /// </summary>
    public static string ToDisplayString(this CompetencyStatus status) => status switch
    {
        CompetencyStatus.Valid => "Gyldig",
        CompetencyStatus.ExpiringSoon => "Utløper snart",
        CompetencyStatus.Expired => "Utgått",
        CompetencyStatus.Revoked => "Tilbakekalt",
        _ => status.ToString()
    };
    
    /// <summary>
    /// Oversetter fra engelsk til norsk
    /// </summary>
    public static string ToDisplayString(this DocumentTargetMode mode) => mode switch
    {
        DocumentTargetMode.None    => "Alle brukere",
        DocumentTargetMode.Department => "Avdeling",
        DocumentTargetMode.JobTitle   => "Stillingstittel",
        _ => mode.ToString()
    };
    
    /// <summary>
    /// Generisk metode for å oversette enums til norsk når EnumType kan variere
    /// </summary>
    /// <param name="value">Den valgte verdien til en enum. (feks EmploymentType.Permanent)</param>
    /// <typeparam name="T">Hvilken enumtype (feks EmploymentType)</typeparam>
    /// <returns>Enum-verdien til en string</returns>
    public static string ToDisplayString<T>(this T value) where T : struct, Enum => value switch
    {
        EmploymentType e => e.ToDisplayString(),
        CompetencyStatus s => s.ToDisplayString(),
        DocumentTargetMode d    => d.ToDisplayString(),
        _ => value.ToString()
    };
}