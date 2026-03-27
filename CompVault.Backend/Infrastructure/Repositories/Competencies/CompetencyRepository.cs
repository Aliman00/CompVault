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
            .Include(c => c.ApplicationUser!)
                .ThenInclude(u => u.Department)
            .Include(c => c.CompetencyType)
            .Where(c => c.Status == CompetencyStatus.ExpiringSoon || c.Status == CompetencyStatus.Expired);

        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);

        if (departmentId.HasValue)
            query = query.Where(c => c.ApplicationUser!.DepartmentId == departmentId.Value);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competency>> GetAllForStatusUpdateAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(c => c.CompetencyType)
            .Where(c => c.Status != CompetencyStatus.Revoked)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task UpdateStatusesAsync(IEnumerable<(Guid Id, CompetencyStatus NewStatus)> updates, CancellationToken cancellationToken = default)
    {
        var updatesList = updates.ToList();

        if (updatesList.Count == 0)
            return;

        // Bygg dictionary for O(1) oppslag
        var updateMap = updatesList.ToDictionary(u => u.Id, u => u.NewStatus);

        // Hent ID-ene vi skal oppdatere
        List<Competency> competencies = await DbSet
            .Where(c => updateMap.Keys.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (Competency competency in competencies)
            competency.Status = updateMap[competency.Id];

        await DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task SoftDeleteAsync(Competency competency, CancellationToken cancellationToken = default)
    {
        competency.DeletedAt = DateTime.UtcNow;
        competency.IsActive = false;
        return Task.CompletedTask;
    }
}
