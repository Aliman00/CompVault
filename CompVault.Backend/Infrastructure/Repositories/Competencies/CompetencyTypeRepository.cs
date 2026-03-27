using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Competencies;

/// <summary>
/// EF Core-implementasjon av <see cref="ICompetencyTypeRepository"/>.
/// </summary>
public sealed class CompetencyTypeRepository(AppDbContext dbContext) : BaseRepository<CompetencyType>(dbContext), ICompetencyTypeRepository
{
    /// <inheritdoc />
    public async Task<CompetencyType?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Name.ToLower() == name.ToLower(), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasActiveCompetenciesAsync(Guid competencyTypeId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<Competency>()
            .AnyAsync(c => c.CompetencyTypeId == competencyTypeId && c.DeletedAt == null, cancellationToken);

    /// <inheritdoc />
    public Task SoftDeleteAsync(CompetencyType competencyType, CancellationToken cancellationToken = default)
    {
        competencyType.DeletedAt = DateTime.UtcNow;
        competencyType.IsActive = false;
        return Task.CompletedTask;
    }
}
