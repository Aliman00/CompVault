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
    /// Generisk metode for å oversette enums til norsk når EnumType kan variere
    /// </summary>
    /// <param name="value">Den valgte verdien til en enum. (feks EmploymentType.Permanent)</param>
    /// <typeparam name="T">Hvilken enumtype (feks EmploymentType)</typeparam>
    /// <returns>Enum-verdien til en string</returns>
    public static string ToDisplayString<T>(this T value) where T : struct, Enum => value switch
    {
        EmploymentType e => e.ToDisplayString(),
        _ => value.ToString()
    };
}