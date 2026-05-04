using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Equipment;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Equipment;

/// <summary>
/// EF Core-implementasjon av <see cref="IEquipmentIssuanceRepository"/>.
/// </summary>
public sealed class EquipmentIssuanceRepository(
    AppDbContext dbContext,
    IDepartmentScopeService departmentScope)
    : BaseRepository<EquipmentIssuance>(dbContext), IEquipmentIssuanceRepository
{
    /// <inheritdoc />
    public async Task<EquipmentIssuance?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        IQueryable<EquipmentIssuance> query = DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null)
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
            .ThenInclude(item => item.Category)
            .Include(i => i.IssuedBy);

        return await ApplyDepartmentFilter(query)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EquipmentIssuance?> GetForUpdateAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        IQueryable<EquipmentIssuance> query = DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null)
            .Include(i => i.Item!)
            .ThenInclude(item => item.Category)
            .Include(i => i.User)
            .Include(i => i.IssuedBy);

        return await ApplyDepartmentFilter(query)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

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
            .Include(i => i.Item!).ThenInclude(item => item.Category)
            .Include(i => i.IssuedBy)
            .AsNoTracking()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentIssuance>> GetByItemIdAsync(
        Guid itemId, CancellationToken ct = default)
    {
        IQueryable<EquipmentIssuance> query = DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null && i.ItemId == itemId)
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Item!)
            .ThenInclude(item => item.Category)
            .Include(i => i.IssuedBy);

        return await ApplyDepartmentFilter(query).ToListAsync(ct);
    }

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
    public IQueryable<EquipmentIssuance> QueryWithDetails()
    {
        IQueryable<EquipmentIssuance> query = DbSet
            .IgnoreQueryFilters()
            .Where(i => i.DeletedAt == null)
            .Include(i => i.User)
            .Include(i => i.Item!)
            .ThenInclude(item => item.Category)
            .Include(i => i.IssuedBy);

        return ApplyDepartmentFilter(query);
    }
    
    /// <inheritdoc />
    public Task SoftDeleteAsync(EquipmentIssuance issuance)
    {
        issuance.DeletedAt = DateTime.UtcNow;
        issuance.IsActive = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Filtrerer vekk utleveringer brukeren ikke har tilattelse til å se/endre
    /// </summary>
    private IQueryable<EquipmentIssuance> ApplyDepartmentFilter(IQueryable<EquipmentIssuance> query)
    {
        if (departmentScope.HasBypass(Permissions.EquipmentAll))
            return query;

        IReadOnlyList<Guid> allowedIds =
            departmentScope.GetAllowedDepartmentIds(Permissions.EquipmentReadSub);

        IQueryable<Guid> allowedUserIds = DbContext.Users
            .IgnoreQueryFilters()
            .Where(u => u.DeletedAt == null && allowedIds.Contains(u.DepartmentId))
            .Select(u => u.Id);

        return query.Where(i => allowedUserIds.Contains(i.UserId));
    }
}