using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Auth.Services;

public interface IRefreshTokenService
{

    /// <summary>
    /// Oppretter et RefreshToken og setter det til lagring i databasen.
    /// Kalleren styrer lagringen
    /// </summary>
    /// <param name="userId">Brukeren vi oppretter Refresh Token for</param>
    /// <param name="ct"></param>
    /// <returns>Et Result med en Refresh Token som string</returns>
    Task<Result<string>> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lager et tilfeldig refresh token (base64-kodet).</summary>
    /// <returns>Refresh token-strengen.</returns>
    string GenerateRefreshToken();
}