using CompVault.Backend.Infrastructure.Repositories.Auth;

namespace CompVault.Backend.Infrastructure.Maintenance;

/// <summary>
/// Service som håndterer cleanup av utgåtte OTP-kode og RefreshTokens
/// </summary>
public class TokenCleanupService(
    IRefreshTokenRepository refreshTokenRepository,
    IOtpCodeRepository otpCodeRepository,
    ILogger<TokenCleanupService> logger) : ITokenCleanupService
{
    /// <inheritdoc />
    public async Task RunCleanupAsync(CancellationToken ct)
    {
        try
        {
            await refreshTokenRepository.DeleteExpiredTokensAsync(ct);
            await otpCodeRepository.DeleteExpiredCodesAsync(ct);
            logger.LogInformation("TokenCleanupService: Cleanup completed");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "TokenCleanupService: Failure under cleanup");
        }
    }
}
