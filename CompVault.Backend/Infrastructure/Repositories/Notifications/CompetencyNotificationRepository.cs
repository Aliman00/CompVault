using CompVault.Backend.Domain.Entities.Notifications;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Notifications;

internal sealed class CompetencyNotificationRepository(AppDbContext dbContext) : ICompetencyNotificationRepository
{
    private readonly DbSet<CompetencyNotificationLog> _dbSet = dbContext.Set<CompetencyNotificationLog>();

    public Task<bool> HasBeenSentAsync(
        Guid competencyId,
        int thresholdDays,
        string recipientEmail,
        CancellationToken ct = default) =>
        _dbSet.AnyAsync(l =>
            l.CompetencyId == competencyId &&
            l.ThresholdDays == thresholdDays &&
            l.RecipientEmail == recipientEmail, ct);

    public async Task AddAsync(CompetencyNotificationLog log, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(log, ct);
    }

    public async Task DeleteForCompetencyAsync(Guid competencyId, CancellationToken ct = default)
    {
        await _dbSet
            .Where(l => l.CompetencyId == competencyId)
            .ExecuteDeleteAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        dbContext.SaveChangesAsync(ct);
}