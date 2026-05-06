using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Equipment;

/// <summary>
/// EF Core-implementasjon av <see cref="IEquipmentItemRepository"/>.
/// </summary>
public sealed class EquipmentItemRepository(AppDbContext dbContext)
    : BaseRepository<EquipmentItem>(dbContext), IEquipmentItemRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentItem>> GetAllWithCategoryAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(i => i.Category)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<EquipmentItem?> GetByIdWithCategoryAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<EquipmentItem?> GetByIdTrackedAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentItem>> GetByCategoryIdAsync(
        Guid categoryId, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(i => i.Category)
            .Where(i => i.CategoryId == categoryId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasActiveIssuancesAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        await DbContext.EquipmentIssuances.AnyAsync(i => i.ItemId == itemId && i.IsActive, cancellationToken);

    /// <inheritdoc />
    public Task SoftDeleteAsync(EquipmentItem item)
    {
        item.DeletedAt = DateTime.UtcNow;
        item.IsActive = false;
        return Task.CompletedTask;
    }
}