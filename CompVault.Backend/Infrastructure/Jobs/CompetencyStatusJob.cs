using System.Text.Json;

using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Infrastructure.Jobs;

/// <summary>
/// Bakgrunnsjobb som oppdaterer status på alle kompetansebevis én gang i døgnet.
/// Berører aldri bevis med status Revoked — disse oppdateres kun manuelt.
/// Logger alle statusendringer i AuditLog manuelt siden ExecuteUpdateAsync
/// går utenom ChangeTracker.
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
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            (int expiredCount, int expiringSoonCount, List<(Guid CompetencyId, CompetencyStatus OldStatus, CompetencyStatus NewStatus)>? statusChanges) = await competencyRepository.UpdateExpiryStatusesAsync(ct);
            int totalUpdated = expiredCount + expiringSoonCount;

            // Opprett AuditLog-entries manuelt for statusendringer fra bakgrunnsjobb
            if (statusChanges.Count > 0)
            {
                var auditEntries = statusChanges.Select(change => new AuditLog
                {
                    Action = "competency.status_auto_update",
                    EntityType = "Competency",
                    EntityId = change.CompetencyId,
                    UserId = null,
                    UserName = "System",
                    Details = JsonSerializer.Serialize(new
                    {
                        old_status = change.OldStatus.ToString(),
                        new_status = change.NewStatus.ToString(),
                        trigger = "expiry_check_job"
                    }),
                }).ToList();

                dbContext.AuditLogs.AddRange(auditEntries);
                await dbContext.SaveChangesAsync(ct);
            }

            logger.LogInformation(
                "Kompetansestatusjobb: {Updated} oppdatert ({Expired} utløpte, {ExpiringSoon} utløper snart, {AuditEntries} revisjonsentries)",
                totalUpdated, expiredCount, expiringSoonCount, statusChanges.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Kompetansestatusjobb: feil under statusoppdatering av kompetansebevis");
        }
    }
}