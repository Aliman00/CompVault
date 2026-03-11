using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Data.Repositories.Identity;

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
    public async Task<IReadOnlyList<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApplicationUser>> GetDirectReportsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Where(u => u.ManagerId == managerId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task SoftDeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await DbSet.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null) return;

        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
    }
}
