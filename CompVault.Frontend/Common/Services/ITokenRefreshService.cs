using CompVault.Frontend.Common.Http.Models;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Services;

public interface ITokenRefreshService
{
    /// <summary>
    /// Refresher token par for innlogget bruker. Parallelle kall venter på samme refresh-operasjon,
    /// slik at både CookieValidationEvents og AccessTokenHandler ikke kjører om hverandre
    /// </summary>
    /// <param name="userId">Brukerens ID fra claim</param>
    /// <param name="refreshToken">Refresh token hentet fra cookie eller CircuitUserContext</param>
    /// <param name="ct"></param>
    /// <returns>Result med RefreshRecord som inneholder token-par og tiden de ble satt</returns>
    Task<Result<RefreshRecord>> RefreshPairAsync(string userId, string refreshToken, CancellationToken ct = default);
    
    /// <summary>
    /// Invaldierer cooldown for en bruker for å manuelt refreshe token
    /// </summary>
    /// <param name="userId"></param>
    void InvalidateCooldown(string userId);
}