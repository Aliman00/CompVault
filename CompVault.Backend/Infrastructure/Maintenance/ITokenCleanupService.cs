namespace CompVault.Backend.Infrastructure.Maintenance;

public interface ITokenCleanupService
{
    /// <summary>
    /// Utfører opprydding av utgåtte OTP-koder og RefreshToken. Kalles av TokenCleanupJob
    /// </summary>
    Task RunCleanupAsync(CancellationToken ct);
}