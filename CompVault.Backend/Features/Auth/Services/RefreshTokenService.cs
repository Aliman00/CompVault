using System.Security.Cryptography;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Shared.Result;
using Microsoft.Extensions.Options;

namespace CompVault.Backend.Features.Auth.Services;

public sealed class RefreshTokenService(
    IJwtService jwtService,
    IRefreshTokenRepository refreshTokenRepository) : IRefreshTokenService
{
    /// <inheritdoc />
    public async Task<Result<string>> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        // Generer tokens;
        string rawRefreshToken = GenerateRefreshToken();

        // Lagrer refresh token i databasen slik at vi kan validere og revoker det senere
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = rawRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(jwtService.RefreshTokenLifetimeDays)
        };
        await refreshTokenRepository.AddAsync(refreshToken, ct);

        return Result<string>.Success(rawRefreshToken);
    }


    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}