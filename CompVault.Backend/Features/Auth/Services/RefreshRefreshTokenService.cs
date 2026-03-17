using System.Security.Cryptography;

namespace CompVault.Backend.Features.Auth.Services;

public class RefreshRefreshTokenService : IRefreshTokenService
{
    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        byte[] randomBytes = new byte[64];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}