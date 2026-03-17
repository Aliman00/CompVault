namespace CompVault.Backend.Features.Auth.Services;

public interface IRefreshTokenService
{
    /// <summary>Lager et tilfeldig refresh token (base64-kodet).</summary>
    /// <returns>Refresh token-strengen.</returns>
    string GenerateRefreshToken();
}