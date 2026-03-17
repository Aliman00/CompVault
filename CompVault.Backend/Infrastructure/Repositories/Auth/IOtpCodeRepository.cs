using CompVault.Backend.Domain.Entities.Auth;

namespace CompVault.Backend.Infrastructure.Repositories.Auth;

public interface IOtpCodeRepository : IRepository<OtpCode>
{
    /// <summary>
    /// Henter siste ubrukte, gyldige kode for en bruker
    /// </summary>
    /// <returns>En OtpCode eller null</returns>
    Task<OtpCode?> GetActiveCodeAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Sletter alle utgåtte og allerede brukte OTP-koder fra databasen.
    /// Kalles av bakgrunnsjobben for å holde tabellen ryddig.
    /// </summary>
    Task DeleteExpiredCodesAsync(CancellationToken ct = default);
}
