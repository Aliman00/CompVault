using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Equipment;

/// <summary>
/// EF Core-implementasjon av <see cref="IEquipmentIssuanceRepository"/>.
/// </summary>
public sealed class EquipmentIssuanceRepository(AppDbContext dbContext)
    : BaseRepository<EquipmentIssuance>(dbContext), IEquipmentIssuanceRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentIssuance>> GetAllWithDetailsAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null)
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
                .ThenInclude(item => item!.Category)
            .Include(i => i.IssuedBy)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<EquipmentIssuance?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null)
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
                .ThenInclude(item => item!.Category)
            .Include(i => i.IssuedBy)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<EquipmentIssuance?> GetForUpdateAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null)
            .Include(i => i.Item!)
                .ThenInclude(item => item!.Category)
            .Include(i => i.User)
            .Include(i => i.IssuedBy)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentIssuance>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null && i.UserId == userId)
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
                .ThenInclude(item => item!.Category)
            .Include(i => i.IssuedBy)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SoftDeleteAsync(EquipmentIssuance issuance, CancellationToken cancellationToken = default)
    {
        issuance.DeletedAt = DateTime.UtcNow;
        issuance.IsActive = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> SoftDeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                .SetProperty(i => i.IsActive, false), cancellationToken);
    }
}
