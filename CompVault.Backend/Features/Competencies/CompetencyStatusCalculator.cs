using CompVault.Shared.Enums;

namespace CompVault.Backend.Features.Competencies;

/// <summary>
/// Statisk hjelpeklasse for beregning av kompetansestatus basert på utløpsdato.
/// Brukes av både <see cref="Services.CompetencyService"/> og <see cref="Infrastructure.Jobs.CompetencyStatusJob"/>
/// for å sikre konsistent logikk.
/// </summary>
public static class CompetencyStatusCalculator
{
    /// <summary>
    /// Antall dager før utløp hvor status endres fra VALID til EXPIRING_SOON.
    /// Frontend kan bruke DaysUntilExpiry fra DTO for mer granular alvorlighetsgrad.
    /// </summary>
    public const int ExpiringSoonThresholdDays = 90;

    /// <summary>
    /// Beregner kompetansestatus basert på utløpsdato.
    /// </summary>
    /// <param name="expiryDate">Utløpsdato. Null hvis typen ikke krever utløp.</param>
    /// <returns>Beregnet status.</returns>
    public static CompetencyStatus Calculate(DateTime? expiryDate)
    {
        if (expiryDate is null)
            return CompetencyStatus.Valid;

        DateTime now = DateTime.UtcNow;

        if (expiryDate < now)
            return CompetencyStatus.Expired;

        if (expiryDate <= now.AddDays(ExpiringSoonThresholdDays))
            return CompetencyStatus.ExpiringSoon;

        return CompetencyStatus.Valid;
    }
}
