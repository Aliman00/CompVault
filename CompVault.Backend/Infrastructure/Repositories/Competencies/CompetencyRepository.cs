using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Enums;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Competencies;

/// <summary>
/// EF Core-implementasjon av <see cref="ICompetencyRepository"/>.
/// </summary>
public sealed class CompetencyRepository(AppDbContext dbContext) : BaseRepository<Competency>(dbContext), ICompetencyRepository
{
    /// <inheritdoc />
    public async Task<Competency?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(c => c.ApplicationUser)
            .Include(c => c.CompetencyType)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competency>> GetAllWithDetailsAsync(
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Competency> query = DbSet
            .AsNoTracking()
            .Include(c => c.ApplicationUser)
            .Include(c => c.CompetencyType);

        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (competencyTypeId.HasValue)
            query = query.Where(c => c.CompetencyTypeId == competencyTypeId.Value);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competency>> GetExpiringAsync(
        Guid? userId,
        Guid? departmentId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Competency> query = DbSet
            .AsNoTracking()
            // ! nødvendig — EF Core garanterer at navigasjonsegenskapen er lastet etter Include
            .Include(c => c.ApplicationUser!)
                .ThenInclude(u => u.Department)
            .Include(c => c.CompetencyType)
            .Where(c => c.Status == CompetencyStatus.ExpiringSoon || c.Status == CompetencyStatus.Expired);

        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);

        if (departmentId.HasValue)
            // ! nødvendig — Include garanterer at ApplicationUser er lastet
            query = query.Where(c => c.ApplicationUser!.DepartmentId == departmentId.Value);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Competency?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(c => c.ApplicationUser)
            .Include(c => c.CompetencyType)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<(int ExpiredCount, int ExpiringSoonCount)> UpdateExpiryStatusesAsync(CancellationToken cancellationToken = default)
    {
        // Må matche CompetencyStatusCalculator.ExpiringSoonThresholdDays (90)
        const int expiringSoonThresholdDays = 90;

        // Sett Expired: bevis med utløpsdato i fortiden og RequiresExpiration == true
        // Berører aldri Revoked (filtrert i WHERE) eller soft-deleted (global query filter)
        int expiredCount = await DbSet
            .Where(c => c.Status != CompetencyStatus.Revoked
                && c.CompetencyType!.RequiresExpiration
                && c.ExpiryDate != null
                && c.ExpiryDate < DateTime.UtcNow)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CompetencyStatus.Expired), cancellationToken);

        // Sett ExpiringSoon: gyldige bevis med utløpsdato innen 90 dager og RequiresExpiration == true
        // Begrenser til Status == Valid for å unngå å re-markere Expired-bevis
        DateTime threshold = DateTime.UtcNow.AddDays(expiringSoonThresholdDays);
        int expiringSoonCount = await DbSet
            .Where(c => c.Status == CompetencyStatus.Valid
                && c.CompetencyType!.RequiresExpiration
                && c.ExpiryDate != null
                && c.ExpiryDate >= DateTime.UtcNow
                && c.ExpiryDate <= threshold)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CompetencyStatus.ExpiringSoon), cancellationToken);

        return (expiredCount, expiringSoonCount);
    }

    /// <inheritdoc />
    public Task SoftDeleteAsync(Competency competency, CancellationToken cancellationToken = default)
    {
        competency.DeletedAt = DateTime.UtcNow;
        competency.IsActive = false;
        return Task.CompletedTask;
    }
}