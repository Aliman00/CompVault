using CompVault.Backend.Infrastructure.Repositories.Competencies;

namespace CompVault.Backend.Infrastructure.Jobs;

/// <summary>
/// Bakgrunnsjobb som oppdaterer status på alle kompetansebevis én gang i døgnet.
/// Berører aldri bevis med status Revoked — disse oppdateres kun manuelt.
/// </summary>
public class CompetencyStatusJob(
    IServiceScopeFactory scopeFactory,
    ILogger<CompetencyStatusJob> logger) : BackgroundService
{
    // Hvor ofte jobben kjører.
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kompetansestatusjobb startet");

        // Kjør umiddelbart ved oppstart, deretter periodisk.
        await RunStatusUpdateAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
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
            (int expiredCount, int expiringSoonCount) = await competencyRepository.UpdateExpiryStatusesAsync(ct);
            int totalUpdated = expiredCount + expiringSoonCount;

            logger.LogInformation(
                "Kompetansestatusjobb: {Updated} oppdatert ({Expired} utløpte, {ExpiringSoon} utløper snart)",
                totalUpdated, expiredCount, expiringSoonCount);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Kompetansestatusjobb: feil under statusoppdatering av kompetansebevis");
        }
    }
}