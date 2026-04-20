namespace CompVault.Frontend.Common.Services;

public interface IClaimsRefreshService
{
    /// <summary>
    /// Manuelt oppdaterer token-par og endrer claims slik at roller, navn og alt annet i claims endres i sanntid
    /// </summary>
    Task RefreshTokensAsync();
}