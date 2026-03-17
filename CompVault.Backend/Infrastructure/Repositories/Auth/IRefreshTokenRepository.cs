using CompVault.Backend.Domain.Entities.Auth;

namespace CompVault.Backend.Infrastructure.Repositories.Auth;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>
    /// Henter et gyldig (ikke-revokert, ikke-utgått) refresh token basert på token-strengen.
    /// </summary>
    Task<RefreshToken?> GetValidTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Henter alle gyldige refresh tokens for en bruker.
    /// </summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Sletter alle utgåtte og revokerte tokens. Brukes av cleanup-jobben.
    /// </summary>
    Task DeleteExpiredTokensAsync(CancellationToken ct = default);
}
