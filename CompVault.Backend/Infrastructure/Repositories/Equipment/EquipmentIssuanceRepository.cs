using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Equipment;

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
                .ThenInclude(item => item.Category)
            .Include(i => i.IssuedBy)
            .ToListAsync(cancellationToken);
    
    /// <inheritdoc />
    public async Task<(IReadOnlyList<EquipmentIssuance> Items, int TotalCount)> GetByUserIdPagedAsync(
        Guid userId, Guid? categoryId, PagedQuery query, CancellationToken ct = default)
    {
        IQueryable<EquipmentIssuance> q = DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null && i.IsActive && i.UserId == userId);

        if (categoryId.HasValue)
            q = q.Where(i => i.Item!.CategoryId == categoryId.Value);

        int totalCount = await q.CountAsync(ct);

        List<EquipmentIssuance> items = await q
            .OrderByDescending(i => i.IssuedDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .Include(i => i.Item!).ThenInclude(item => item!.Category)
            .Include(i => i.IssuedBy)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentIssuance>> GetByItemIdAsync(
        Guid itemId, CancellationToken ct = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null && i.ItemId == itemId)
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
            .ThenInclude(item => item!.Category)
            .Include(i => i.IssuedBy)
            .ToListAsync(ct);
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserEquipmentCategoryDto>> GetCategoriesForUserAsync(
        Guid userId, CancellationToken ct = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null && i.IsActive && i.UserId == userId)
            .GroupBy(i => new { i.Item!.CategoryId, i.Item.Category!.Name })
            .Select(g => new UserEquipmentCategoryDto
            {
                Id = g.Key.CategoryId,
                Name = g.Key.Name,
                ItemCount = g.Select(i => i.ItemId).Distinct().Count()
            })
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <inheritdoc />
    public IQueryable<EquipmentIssuance> QueryWithDetails() =>
        DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null)
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
                .ThenInclude(item => item!.Category)
            .Include(i => i.IssuedBy);

    /// <inheritdoc />
    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null && i.UserId == userId)
            .CountAsync(cancellationToken);

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
