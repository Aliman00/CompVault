using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Enums;

using MudBlazor;
namespace CompVault.Frontend.Features.Competencies.Extensions;

public static class CompetencyDisplayExtensions
{
    /// <summary>
    /// Viser en setning utifra hvor når et bevis utgår/utgikk
    /// </summary>
    /// <param name="daysUntilExpiry">Antall dager som int</param>
    /// <param name="status">Ingen tekst hvis den er utgått</param>
    /// <returns>Setning basert på hvor lenge til den utgår</returns>
    public static string ToExpiryText(this int? daysUntilExpiry, CompetencyStatus status)
    {
        if (status == CompetencyStatus.Revoked)
            return string.Empty;
        
        return daysUntilExpiry switch
        {
            null => string.Empty,
            < -1 => $"Utløpt for {Math.Abs(daysUntilExpiry.Value)} dager siden",
            -1 => "Utløpt i går",
            0 => "Utløper i dag",
            1 => "Utløper i morgen",
            _ => $"{daysUntilExpiry} dager"
        };
    }

    /// <summary>
    /// Endrer farge i UI-en utifra hvor mange dager igjen til et bevis utgår
    /// </summary>
    /// <param name="daysUntilExpiry">Antall dager som int</param>
    /// <param name="status">Default farge hvis den er utgått</param>
    /// <returns>MudBlazor Color</returns>
    public static Color ToExpiryColor(this int? daysUntilExpiry, CompetencyStatus status)
    {
        if (status == CompetencyStatus.Revoked)
            return Color.Default;
        
        return daysUntilExpiry switch
        {
            null => Color.Default,
            < 0 => Color.Error,
            <= 30 => Color.Warning,
            _ => Color.Default
        };
    }
    
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
        CompetencyStatus.Revoked => Color.Default,
        _ => Color.Default
    };
    
    /// <summary>
    /// Tittelen på detail-siden til Competency. Type bevis og fornavn og etternavn
    /// </summary>
    /// <param name="competencyDto">CompetencyDto</param>
    /// <returns>En string med først type kursbevis og fult navn. Eks: Førstehjelp - Geir Skinkebit </returns>
    public static string ToDetailTitle(this CompetencyDto competencyDto) =>
        $"{competencyDto.CompetencyTypeName} – {competencyDto.FullName}";
    
    /// <summary>
    /// Subtitle på detail-siden til Competency. Status og når den utløper i klar tekst, så fremt den ikke er revoked
    /// </summary>
    /// <param name="dto">CompetencyDto</param>
    /// <returns>En string med først status og deretter når den utgår, eller en string med når den er revoked</returns>
    public static string ToDetailSubtitle(this CompetencyDto dto)
    {
        if (dto.Status == CompetencyStatus.Revoked)
            return $"Tilbakekalt {dto.RevokedAt?.ToString("dd.MM.yyyy") ?? string.Empty}".Trim();

        string expiryText = dto.DaysUntilExpiry.ToExpiryText(dto.Status);
        return string.IsNullOrEmpty(expiryText)
            ? dto.Status.ToDisplayString()
            : $"{dto.Status.ToDisplayString()} – {expiryText}";
    }
}