using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Domain.Entities.Notifications;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Models;
using CompVault.Backend.Infrastructure.Email.Templates;
using CompVault.Backend.Infrastructure.Repositories.Notifications;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Jobs;

/// <summary>
/// Bakgrunnsjobb som sender e-postvarsler til ansatte og deres ledere
/// når et kompetansebevis nærmer seg utløp eller har utløpt.
/// Kjøres én gang i døgnet.
///
/// Varslingsterskler: 90, 60, 30, 14, 7, 0 dager før utløp.
/// En dedupliseringstabell (<see cref="CompetencyNotificationLog"/>) sikrer at
/// hver kombinasjon av (kompetanse, terskel, mottaker) kun varsles én gang.
/// </summary>
public class ExpiryNotificationJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiryNotificationJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    // Terskler i synkende rekkefølge — viktig for at <= logikken fungerer korrekt.
    // Eksperten: hvis daysUntil er 45, vil 90 og 60 trigge samtidig første gang,
    // men begge logges slik at de aldri sendes igjen.
    private static readonly int[] Thresholds = [90, 60, 30, 14, 7, 0];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Utløpsvarslingsjobb startet");

        await RunAsync(stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        IEmailService emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        ICompetencyNotificationRepository notificationRepo = scope.ServiceProvider.GetRequiredService<ICompetencyNotificationRepository>();

        try
        {
            DateTime now = DateTime.UtcNow;

            List<Competency> competencies = await dbContext.Competencies
                .AsNoTracking()
                .Include(c => c.ApplicationUser!)
                    .ThenInclude(u => u.Manager)
                .Include(c => c.CompetencyType)
                .Where(c => c.CompetencyType!.RequiresExpiration
                            && c.ExpiryDate != null
                            && c.Status != CompetencyStatus.Revoked)
                .ToListAsync(ct);

            int sentCount = 0;
            int skippedCount = 0;

            foreach (Competency competency in competencies)
            {
                if (ct.IsCancellationRequested)
                    break;

                // Null-sjekk av navigasjonsegenskaper (inkluderer de, men teoretisk mulig null)
                if (competency.ApplicationUser?.Email is null || competency.CompetencyType is null)
                    continue;

                int daysUntil = (int)(competency.ExpiryDate!.Value.Date - now.Date).TotalDays;

                (int employeeSent, int managerSent, int employeeSkipped, int managerSkipped) =
                    await ProcessCompetencyAsync(
                        competency, daysUntil, emailService, notificationRepo, ct);

                sentCount += employeeSent + managerSent;
                skippedCount += employeeSkipped + managerSkipped;
            }

            logger.LogInformation(
                "Utløpsvarslingsjobb: {Sent} varsler sendt, {Skipped} hoppet over, {TotalChecked} kompetanser sjekket",
                sentCount, skippedCount, competencies.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Utløpsvarslingsjobb: feil under sending av utløpsvarsler");
        }
    }

    private async Task<(int employeeSent, int managerSent, int employeeSkipped, int managerSkipped)>
        ProcessCompetencyAsync(
            Competency competency,
            int daysUntil,
            IEmailService emailService,
            ICompetencyNotificationRepository notificationRepo,
            CancellationToken ct)
    {
        int employeeSent = 0;
        int managerSent = 0;
        int employeeSkipped = 0;
        int managerSkipped = 0;

        foreach (int threshold in Thresholds)
        {
            if (ct.IsCancellationRequested)
                break;

            // Kun terskler som er passert eller akkurat nås
            if (daysUntil > threshold)
                continue;

            // For kompetanser som allerede har utløpt, send kun threshold=0 (Expired).
            // Andre terskler ville vist et negativt antall dager i e-posten.
            if (threshold > 0 && daysUntil < 0)
                continue;

            ApplicationUser? manager = competency.ApplicationUser?.Manager;

            // === Varsel til ansatt ===
            if (competency.ApplicationUser?.Email is not null)
            {
                (bool wasSent, bool wasSkipped) = await TrySendNotificationAsync(
                    competency, threshold, daysUntil,
                    competency.ApplicationUser.Email,
                    "Employee",
                    competency.ApplicationUser.FirstName,
                    emailService, notificationRepo, ct);

                if (wasSent) employeeSent++;
                if (wasSkipped) employeeSkipped++;
            }

            // === Varsel til leder ===
            if (manager?.Email is not null && manager.IsActive && manager.DeletedAt == null)
            {
                (bool wasSent, bool wasSkipped) = await TrySendNotificationAsync(
                    competency, threshold, daysUntil,
                    manager.Email,
                    "Manager",
                    manager.FirstName,
                    emailService, notificationRepo, ct);

                if (wasSent) managerSent++;
                if (wasSkipped) managerSkipped++;
            }
        }

        return (employeeSent, managerSent, employeeSkipped, managerSkipped);
    }

    /// <returns>(wasSent: true hvis ny varsling ble sendt, wasSkipped: true hvis allerede varslet)</returns>
    private async Task<(bool wasSent, bool wasSkipped)> TrySendNotificationAsync(
        Competency competency,
        int threshold,
        int daysUntil,
        string recipientEmail,
        string recipientRole,
        string recipientName,
        IEmailService emailService,
        ICompetencyNotificationRepository notificationRepo,
        CancellationToken ct)
    {
        bool alreadySent = await notificationRepo.HasBeenSentAsync(
            competency.Id, threshold, recipientEmail, ct);

        if (alreadySent)
            return (false, true);

        string competencyName = competency.CompetencyType!.Name;
        DateTime expiryDate = competency.ExpiryDate!.Value;

        EmailBody emailBody = threshold == 0
            ? BuildExpiredEmail(recipientRole, recipientName, competency, competencyName, expiryDate)
            : BuildExpiringSoonEmail(recipientRole, recipientName, competency, competencyName, expiryDate, daysUntil);

        Result result = await emailService.SendAsync(recipientEmail, emailBody, ct);

        if (!result.IsSuccess)
        {
            logger.LogWarning(
                "Utløpsvarslingsjobb: kunne ikke sende varsel ({Threshold}d) til {Email} for competency {CompetencyId}",
                threshold, recipientEmail, competency.Id);
            return (false, false);
        }

        var log = new CompetencyNotificationLog
        {
            CompetencyId = competency.Id,
            ThresholdDays = threshold,
            RecipientEmail = recipientEmail,
            RecipientRole = recipientRole,
            SentAt = DateTime.UtcNow
        };

        await notificationRepo.AddAsync(log, ct);
        await notificationRepo.SaveChangesAsync(ct);

        logger.LogDebug(
            "Utløpsvarslingsjobb: sendt varsel ({Threshold}d) til {Role} {Email} for {CompetencyName}",
            threshold, recipientRole, recipientEmail, competencyName);

        return (true, false);
    }

    private static EmailBody BuildExpiringSoonEmail(
        string recipientRole,
        string recipientName,
        Competency competency,
        string competencyName,
        DateTime expiryDate,
        int daysUntil)
    {
        if (recipientRole == "Manager")
        {
            string employeeName = competency.ApplicationUser?.FirstName ?? "Ansatt";
            return CompetencyEmailTemplates.ExpiringSoonToManager(
                recipientName, employeeName, competencyName, expiryDate, daysUntil);
        }

        return CompetencyEmailTemplates.ExpiringSoonToEmployee(
            recipientName, competencyName, expiryDate, daysUntil);
    }

    private static EmailBody BuildExpiredEmail(
        string recipientRole,
        string recipientName,
        Competency competency,
        string competencyName,
        DateTime expiryDate)
    {
        if (recipientRole == "Manager")
        {
            string employeeName = competency.ApplicationUser?.FirstName ?? "Ansatt";
            return CompetencyEmailTemplates.ExpiredToManager(
                recipientName, employeeName, competencyName, expiryDate);
        }

        return CompetencyEmailTemplates.ExpiredToEmployee(
            recipientName, competencyName, expiryDate);
    }
}
