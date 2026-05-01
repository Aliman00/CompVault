using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Features.Competencies;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Constants;
using CompVault.Shared.Enums;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Competencies;

/// <summary>
/// EF Core-implementasjon av <see cref="ICompetencyRepository"/>.
/// </summary>
public sealed class CompetencyRepository(AppDbContext dbContext, IDepartmentScopeService departmentScope) : 
    BaseRepository<Competency>(dbContext), ICompetencyRepository
{
    /// <inheritdoc />
    public async Task<Competency?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await ApplyDepartmentFilter(DbSet
                .AsNoTracking()
                .Include(c => c.ApplicationUser)
                .Include(c => c.CompetencyType))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competency>> GetAllWithDetailsAsync(
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Competency> query = BuildFilteredQuery(userId, status, competencyTypeId);
        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountWithFiltersAsync(
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default) =>
        await BuildFilteredQuery(userId, status, competencyTypeId).CountAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Competency>> GetAllWithDetailsPagedAsync(
        int skip,
        int take,
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default) =>
        await BuildFilteredQuery(userId, status, competencyTypeId)
            // ! nødvendig — EF Core garanterer at navigasjonsegenskapen er lastet etter Include
            .OrderBy(c => c.ApplicationUser!.LastName)
                .ThenBy(c => c.ApplicationUser!.FirstName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Bygger en IQueryable med navigasjon og valgfrie filtre.
    /// Gjenbrukes av GetAllWithDetailsAsync, CountWithFiltersAsync og GetAllWithDetailsPagedAsync.
    /// </summary>
    private IQueryable<Competency> BuildFilteredQuery(
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId)
    {
        IQueryable<Competency> query = DbSet
            .AsNoTracking()
            .Include(c => c.ApplicationUser)
            .Include(c => c.CompetencyType);
        
        // Filterer vekk avdelinger vi ikke har tilattelse til
        query = ApplyDepartmentFilter(query);
        
        if (userId.HasValue)
            query = query.Where(c => c.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (competencyTypeId.HasValue)
            query = query.Where(c => c.CompetencyTypeId == competencyTypeId.Value);

        return query;
    }

    /// <inheritdoc />
    public async Task<Competency?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(c => c.ApplicationUser)
            .Include(c => c.CompetencyType)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<(int ExpiredCount, int ExpiringSoonCount, List<(Guid CompetencyId, CompetencyStatus OldStatus, CompetencyStatus NewStatus)> StatusChanges)> UpdateExpiryStatusesAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        int expiringSoonThresholdDays = CompetencyStatusCalculator.ExpiringSoonThresholdDays;
        DateTime threshold = now.AddDays(expiringSoonThresholdDays);

        var statusChanges = new List<(Guid CompetencyId, CompetencyStatus OldStatus, CompetencyStatus NewStatus)>();

        // --- Expired: Finn kompetanser som vil bli satt til Expired ---
        // Ekskluderer allerede Expired for å unngå duplikate audit-entries
        var toExpire = await DbSet
            .Where(c => c.Status != CompetencyStatus.Revoked
                && c.Status != CompetencyStatus.Expired
                && c.CompetencyType!.RequiresExpiration
                && c.ExpiryDate != null
                && c.ExpiryDate < now)
            .Select(c => new { c.Id, c.Status })
            .ToListAsync(cancellationToken);

        // Sett Expired via ExecuteUpdateAsync
        int expiredCount = await DbSet
            .Where(c => c.Status != CompetencyStatus.Revoked
                && c.Status != CompetencyStatus.Expired
                && c.CompetencyType!.RequiresExpiration
                && c.ExpiryDate != null
                && c.ExpiryDate < now)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CompetencyStatus.Expired), cancellationToken);

        foreach (var item in toExpire)
            statusChanges.Add((item.Id, item.Status, CompetencyStatus.Expired));

        // --- ExpiringSoon: Finn kompetanser som vil bli satt til ExpiringSoon ---
        var toExpireSoon = await DbSet
            .Where(c => c.Status == CompetencyStatus.Valid
                && c.CompetencyType!.RequiresExpiration
                && c.ExpiryDate != null
                && c.ExpiryDate >= now
                && c.ExpiryDate <= threshold)
            .Select(c => new { c.Id, c.Status })
            .ToListAsync(cancellationToken);

        int expiringSoonCount = await DbSet
            .Where(c => c.Status == CompetencyStatus.Valid
                && c.CompetencyType!.RequiresExpiration
                && c.ExpiryDate != null
                && c.ExpiryDate >= now
                && c.ExpiryDate <= threshold)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CompetencyStatus.ExpiringSoon), cancellationToken);

        foreach (var item in toExpireSoon)
            statusChanges.Add((item.Id, item.Status, CompetencyStatus.ExpiringSoon));

        return (expiredCount, expiringSoonCount, statusChanges);
    }

    /// <inheritdoc />
    public Task SoftDeleteAsync(Competency competency, CancellationToken cancellationToken = default)
    {
        competency.DeletedAt = DateTime.UtcNow;
        competency.IsActive = false;
        return Task.CompletedTask;
    }
    
    // =========================== Hjelpemetoder =========================== 
    
    // Filter som sjekker at vi ikke kan hente kompetansebevis vi ikke har tilattelse til
    private IQueryable<Competency> ApplyDepartmentFilter(IQueryable<Competency> query)
    {
        if (departmentScope.HasBypass(Permissions.CompetenciesAll))
            return query;

        IReadOnlyList<Guid> allowedIds =
            departmentScope.GetAllowedDepartmentIds(Permissions.CompetenciesReadSub);

        IQueryable<Guid> allowedUserIds = DbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.DeletedAt == null && allowedIds.Contains(u.DepartmentId))
            .Select(u => u.Id);

        return query.Where(c => allowedUserIds.Contains(c.UserId));
    }
}