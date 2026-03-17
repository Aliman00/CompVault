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
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<(ApplicationUser User, List<string> Roles)>> GetActiveUsersWithRolesAsync(CancellationToken cancellationToken = default)
    {
        var result = await DbSet
            .AsNoTracking()
            .Where(u => u.IsActive && u.DeletedAt == null)
            .Select(u => new
            {
                User = u,
                Roles = DbContext.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(DbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .Where(name => name != null)
                    .Select(name => name!)
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return result.Select(x => (x.User, x.Roles)).ToList();
    }

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
    public Task SoftDeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        return Task.CompletedTask;
    }
}
