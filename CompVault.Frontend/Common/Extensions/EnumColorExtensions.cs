using CompVault.Shared.Enums;
using MudBlazor;
namespace CompVault.Frontend.Common.Extensions;

/// <summary>
/// Metoder for visning av enum i forskjellige farger
/// </summary>
public static class EnumColorExtensions
{
    /// <summary>
    /// Viser status fargen til kompetansebevis i forskjellige farger
    /// </summary>
    /// <param name="status">CompetencyStatus</param>
    /// <returns>En MudBlazor farge</returns>
    public static Color ToStatusColor(this CompetencyStatus status) => status switch
    {
        CompetencyStatus.Valid => Color.Success,
        CompetencyStatus.ExpiringSoon => Color.Warning,
        CompetencyStatus.Expired => Color.Error,
        CompetencyStatus.Revoked => Color.Dark,
        _ => Color.Default
    };
}