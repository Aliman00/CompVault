using CompVault.Backend.Domain.Entities.Competencies;

namespace CompVault.Backend.Domain.Entities.Notifications;

/// <summary>
/// Sporer sendte utløpsvarslinger per kompetansebevis, terskel og mottaker.
/// Brukes av <see cref="Infrastructure.Jobs.ExpiryNotificationJob"/> for å unngå
/// duplikate varslinger — hver kombinasjon (competency, threshold, epost) sendes kun én gang.
/// </summary>
public class CompetencyNotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK til kompetansebeviset varslingen gjelder.</summary>
    public Guid CompetencyId { get; set; }

    /// <summary>
    /// Hvilken terskel som utløste varslingen.
    /// 90, 60, 30, 14, 7, eller 0 dager før utløp.
    /// </summary>
    public int ThresholdDays { get; set; }

    /// <summary>E-postadressen som mottok varslingen.</summary>
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>"Employee" eller "Manager".</summary>
    public string RecipientRole { get; set; } = string.Empty;

    /// <summary>Når varslingen ble sendt (UTC).</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // ======================== Navigasjonsegenskaper ========================

    public Competency Competency { get; set; } = null!;
}
