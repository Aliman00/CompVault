using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Enums;
namespace CompVault.Frontend.Common.Extensions;


/// <summary>
/// Extensions for å sortere DTOer og objekter
/// </summary>
public static class SortingExtensions
{
    /// <summary>
    /// Sorterer etter status og deretter dager til de utgår
    /// </summary>
    /// <param name="competencies">Liste med CompetencyDto-er</param>
    /// <returns>Sortert liste</returns>
    public static List<CompetencyDto> OrderByStatus(this IEnumerable<CompetencyDto> competencies)
        => competencies
        .OrderBy(c => c.Status switch
        {
            CompetencyStatus.Expired => 0,
            CompetencyStatus.ExpiringSoon => 1,
            CompetencyStatus.Valid => 2,
            CompetencyStatus.Revoked => 3,
            _ => 4
        })
        .ThenBy(c => c.DaysUntilExpiry ?? int.MaxValue)
        .ToList();
}