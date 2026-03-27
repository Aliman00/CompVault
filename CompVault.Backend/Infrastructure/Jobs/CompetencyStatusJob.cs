using CompVault.Backend.Features.Competencies;
using CompVault.Backend.Features.Competencies.Services;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Infrastructure.Jobs;

/// <summary>
/// Bakgrunnsjobb som oppdaterer status på alle kompetansebevis én gang i døgnet.
/// Berører aldri bevis med status Revoked — disse oppdateres kun manuelt.
/// </summary>
public class CompetencyStatusJob(
    IServiceScopeFactory scopeFactory,
    ILogger<CompetencyStatusJob> logger) : BackgroundService
{
    /// <summary>Hvor ofte jobben kjører.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kompetansestatusjobb startet");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Venter til neste kjøring før vi starter — unngår at jobben kjører umiddelbart ved oppstart
            await Task.Delay(Interval, stoppingToken);

            await RunStatusUpdateAsync(stoppingToken);
        }
    }

    // Bruker et nytt scope siden BackgroundService er singleton mens repository er scoped.
    private async Task RunStatusUpdateAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        ICompetencyRepository competencyRepository = scope.ServiceProvider.GetRequiredService<ICompetencyRepository>();

        try
        {
            IReadOnlyList<Domain.Entities.Competencies.Competency> competencies =
                await competencyRepository.GetAllForStatusUpdateAsync(ct);

            var updates = new List<(Guid Id, CompetencyStatus NewStatus)>();
            int validCount = 0;
            int expiredCount = 0;
            int expiringSoonCount = 0;

            foreach (Domain.Entities.Competencies.Competency competency in competencies)
            {
                // Hent typens RequiresExpiration for å avgjøre om utløpsdato er relevant
                bool requiresExpiration = competency.CompetencyType?.RequiresExpiration ?? true;
                DateTime? expiryDate = requiresExpiration ? competency.ExpiryDate : null;

                CompetencyStatus newStatus = CompetencyStatusCalculator.Calculate(expiryDate);

                if (competency.Status != newStatus)
                {
                    updates.Add((competency.Id, newStatus));

                    switch (newStatus)
                    {
                        case CompetencyStatus.Valid:
                            validCount++;
                            break;
                        case CompetencyStatus.Expired:
                            expiredCount++;
                            break;
                        case CompetencyStatus.ExpiringSoon:
                            expiringSoonCount++;
                            break;
                    }
                }
            }

            if (updates.Count > 0)
                await competencyRepository.UpdateStatusesAsync(updates, ct);

            logger.LogInformation(
                "Kompetansestatusjobb: {Total} sjekket, {Updated} oppdatert ({Valid} gyldige, {Expired} utløpte, {ExpiringSoon} utløper snart)",
                competencies.Count, updates.Count, validCount, expiredCount, expiringSoonCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Kompetansestatusjobb: feil under statusoppdatering av kompetansebevis");
        }
    }
}
