using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Identity;

/// <summary>
/// EF Core-implementasjon av <see cref="IRoleRepository"/>.
/// </summary>
public sealed class RoleRepository(AppDbContext dbContext) : BaseRepository<ApplicationRole>(dbContext), IRoleRepository
{
    /// <inheritdoc />
    public async Task<ApplicationRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationRole>> GetAllWithPermissionsAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasUsersAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await DbContext.UserRoles.AnyAsync(ur => ur.RoleId == roleId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetPermissionNamesForRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await DbContext.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetPermissionsByNamesAsync(HashSet<string> names, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(names);
        if (names.Count == 0)
            return [];
        return await DbContext.Permissions
            .AsNoTracking()
            .Where(p => names.Contains(p.Name))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        List<RolePermission> existing = await DbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);
        DbContext.RolePermissions.RemoveRange(existing);
    }

    /// <inheritdoc />
    public async Task AddRolePermissionsAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rolePermissions);
        await DbContext.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default) =>
        await DbContext.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
}