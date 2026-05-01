using CompVault.Backend.Domain.Entities.Notifications;

namespace CompVault.Backend.Infrastructure.Repositories.Notifications;

/// <summary>
/// Repository for varslingslogg. Brukes av <see cref="Jobs.ExpiryNotificationJob"/>
/// for å sjekke om et varsel allerede er sendt og for å registrere nye varslinger.
/// Brukes også av <see cref="Features.Competencies.Services.CompetencyService"/> for
/// å slette gammel varslingslogg når et kompetansebevis fornyes (ExpiryDate endres).
/// </summary>
public interface ICompetencyNotificationRepository
{
    /// <summary>
    /// Sjekker om det allerede finnes en varslingslogg for denne kombinasjonen.
    /// </summary>
    Task<bool> HasBeenSentAsync(
        Guid competencyId,
        int thresholdDays,
        string recipientEmail,
        CancellationToken ct = default);

    /// <summary>
    /// Registrerer at et varsel er sendt.
    /// </summary>
    Task AddAsync(CompetencyNotificationLog log, CancellationToken ct = default);

    /// <summary>
    /// Sletter all varslingslogg for et kompetansebevis.
    /// Kalles når ExpiryDate endres (fornyelse) slik at varslingssyklusen starter på nytt.
    /// </summary>
    Task DeleteForCompetencyAsync(Guid competencyId, CancellationToken ct = default);

    /// <summary>
    /// Persisterer endringer.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}