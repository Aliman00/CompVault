using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Auth;

public class RefreshTokenRepository(AppDbContext context)
    : BaseRepository<RefreshToken>(context), IRefreshTokenRepository
{
    /// <inheritdoc />
    public async Task<RefreshToken?> GetValidTokenAsync(string token, CancellationToken ct = default) =>
        await DbSet
            .Where(r => r.Token == token && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Where(r => r.UserId == userId && !r.IsRevoked && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task DeleteExpiredTokensAsync(CancellationToken ct = default) =>
        await DbSet
            .Where(r => r.ExpiresAt < DateTime.UtcNow || r.IsRevoked)
            .ExecuteDeleteAsync(ct);
}
