using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Equipment;

/// <summary>
/// EF Core-implementasjon av <see cref="IEquipmentCategoryRepository"/>.
/// </summary>
public sealed class EquipmentCategoryRepository(AppDbContext dbContext)
    : BaseRepository<EquipmentCategory>(dbContext), IEquipmentCategoryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentCategory>> GetAllWithItemsAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(c => c.Items)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<EquipmentCategory?> GetByIdWithItemsAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<EquipmentCategory?> GetByIdWithItemsForUpdateAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task SoftDeleteAsync(EquipmentCategory category, CancellationToken cancellationToken = default)
    {
        category.DeletedAt = DateTime.UtcNow;
        category.IsActive = false;
        return Task.CompletedTask;
    }
}