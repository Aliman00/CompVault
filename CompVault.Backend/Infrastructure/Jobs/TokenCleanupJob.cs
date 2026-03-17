using CompVault.Backend.Infrastructure.Repositories.Auth;

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

            await RunCleanupAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Utfører selve oppryddingen. Bruker et nytt scope siden BackgroundService er singleton
    /// mens repository og UnitOfWork er scoped.
    /// </summary>
    private async Task RunCleanupAsync(CancellationToken ct)
    {
        // BackgroundService er singleton — vi må lage et nytt scope for å få tak i scoped tjenester
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        var otpCodeRepository = scope.ServiceProvider.GetRequiredService<IOtpCodeRepository>();

        try
        {
            // ExecuteDeleteAsync sender SQL direkte mot DB — SaveChanges er ikke nødvendig
            await refreshTokenRepository.DeleteExpiredTokensAsync(ct);
            await otpCodeRepository.DeleteExpiredCodesAsync(ct);

            logger.LogInformation("TokenCleanupJob: utgåtte refresh tokens og OTP-koder er slettet");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Logger feilen men lar jobben fortsette — neste kjøring vil prøve igjen
            logger.LogError(ex, "TokenCleanupJob: feil under opprydding av tokens");
        }
    }
}
