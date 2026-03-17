using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Auth;

public class OtpCodeRepository(AppDbContext context) : BaseRepository<OtpCode>(context), IOtpCodeRepository
{
    /// <inheritdoc />
    public async Task<OtpCode?> GetActiveCodeAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Where(o => o.UserId == userId && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public async Task DeleteExpiredCodesAsync(CancellationToken ct = default) =>
        await DbSet
            .Where(o => o.IsUsed || o.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);

}
