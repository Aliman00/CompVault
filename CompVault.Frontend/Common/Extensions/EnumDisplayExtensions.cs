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
}