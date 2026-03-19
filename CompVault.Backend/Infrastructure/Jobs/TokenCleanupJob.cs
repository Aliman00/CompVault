using CompVault.Backend.Infrastructure.Maintenance;


namespace CompVault.Backend.Infrastructure.Jobs;

/// <summary>
/// Bakgrunnsjobb som periodisk rydder opp utgåtte og revokerte tokens og koder fra databasen.
/// Kjører én gang i døgnet for å holde tabellene ryddig.
/// Håndterer: RefreshToken og OtpCode.
/// </summary>
public class TokenCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<TokenCleanupJob> logger) : BackgroundService
{
    // Hvor ofte jobben kjører
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TokenCleanupJob startet");

        // Kjører kontinuerlig til applikasjonen stoppes
        while (!stoppingToken.IsCancellationRequested)
        {
            // Venter til neste kjøring før vi starter — unngår at jobben kjører umiddelbart ved oppstart
            await Task.Delay(Interval, stoppingToken);
            
            await using var scope = scopeFactory.CreateAsyncScope();
            var cleanupService = scope.ServiceProvider
                .GetRequiredService<ITokenCleanupService>();
            
            await cleanupService.RunCleanupAsync(stoppingToken);
        }
    }
}
