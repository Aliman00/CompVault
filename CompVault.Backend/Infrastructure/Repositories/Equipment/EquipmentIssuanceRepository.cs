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
            .Include(i => i.Item!)
                .ThenInclude(item => item!.Category)
            .Include(i => i.User)
            .Include(i => i.IssuedBy)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentIssuance>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
                .ThenInclude(item => item!.Category)
            .Include(i => i.IssuedBy)
            .Where(i => i.UserId == userId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SoftDeleteAsync(EquipmentIssuance issuance, CancellationToken cancellationToken = default)
    {
        issuance.DeletedAt = DateTime.UtcNow;
        issuance.IsActive = false;
        return Task.CompletedTask;
    }
}