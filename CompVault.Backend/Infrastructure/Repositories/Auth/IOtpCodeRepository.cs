using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Infrastructure.Data.Repositories;

namespace CompVault.Backend.Infrastructure.Repositories.Auth;

public interface IOtpCodeRepository : IRepository<OtpCode>
{
    /// <summary>
    /// Henter siste ubrukte, gyldige kode for en bruker
    /// </summary>
    /// <returns>En OtpCode eller null</returns>
    Task<OtpCode?> GetActiveCodeAsync(Guid userId, CancellationToken ct = default);
}