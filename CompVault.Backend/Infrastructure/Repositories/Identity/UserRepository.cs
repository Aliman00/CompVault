using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Identity;

/// <summary>
/// EF Core-implementasjon av <see cref="IUserRepository"/>.
/// </summary>
public sealed class UserRepository(AppDbContext dbContext) : BaseRepository<ApplicationUser>(dbContext), IUserRepository
{
    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant() && u.DeletedAt == null, cancellationToken);
    
    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByIdIgnoringFiltersAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null, ct);
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<(ApplicationUser User, List<string> Roles)>>
        GetActiveUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.JobTitle)
            .Where(u => u.IsActive && u.DeletedAt == null)
            .Select(u => new
            {
                User = u,
                Roles = DbContext.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(DbContext.Roles, ur => ur.RoleId, r => r.Id,
                        (ur, r) => r.Name)
                    .Where(name => name != null)
                    .Select(name => name!)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.User, x.Roles)).ToList();
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationUser>> GetUsersByTargetAsync(IReadOnlyList<Guid> departmentIds,
        IReadOnlyList<Guid> jobTitleIds, CancellationToken ct = default) =>
        await DbSet
            .AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.JobTitle)
            .Where(u => departmentIds.Count == 0 ||
                        (u.DepartmentId.HasValue && departmentIds.Contains(u.DepartmentId.Value)))
            .Where(u => jobTitleIds.Count == 0 ||
                        (u.JobTitleId.HasValue && jobTitleIds.Contains(u.JobTitleId.Value)))
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<ApplicationUser?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        await DbSet
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.JobTitle)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Where(u => u.IsActive)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationUser>> GetDirectReportsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Where(u => u.ManagerId == managerId && u.IsActive)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationUser>> GetPotentialManagersAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.JobTitle)
            .Where(u => u.IsActive
                        && u.DeletedAt == null
                        && u.JobTitle != null
                        && u.JobTitle.IsLeader)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task SoftDeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<int> CountActiveAsync(CancellationToken cancellationToken = default) =>
        await DbSet.CountAsync(u => u.IsActive && u.DeletedAt == null, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<(ApplicationUser User, List<string> Roles)>> GetActiveUsersWithRolesPagedAsync(
        int skip, int take, CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .AsNoTracking()
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.JobTitle)
            .Where(u => u.IsActive && u.DeletedAt == null)
            .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
            .Skip(skip)
            .Take(take)
            .Select(u => new
            {
                User = u,
                Roles = DbContext.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(DbContext.Roles, ur => ur.RoleId, r => r.Id,
                        (ur, r) => r.Name)
                    .Where(name => name != null)
                    .Select(name => name!)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.User, x.Roles)).ToList();
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationUser>> GetLookupAsync(IReadOnlyList<Guid> allowedDepartmentIds,
        bool bypass, CancellationToken ct = default) =>
        await DbSet
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(u => u.Department)
            .Include(u => u.JobTitle)
            .Where(u => u.IsActive && u.DeletedAt == null)
            .Where(u => bypass ||
                        (u.DepartmentId.HasValue && allowedDepartmentIds.Contains(u.DepartmentId.Value)))
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(ct);
}