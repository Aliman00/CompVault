using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Auth.Services;

public interface IOtpCodeService
{
    /// <summary>
    /// Oppretter en 6-sifret kode og oppretter en OtpCode for databasen med hashet kode.
    /// Vi ønsker ikke å lagre koden i klartekst. Koden lever like lenge som vi setter appsettings
    /// </summary>
    /// <param name="userId">Brukeren som skal få en opprettet kode</param>
    /// <param name="ct"></param>
    /// <returns>6-sifret kode</returns>
    Task<Result<string>> GenerateOtpCodeAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Verifiserer en OTP-kode for en bruker.
    /// Sjekker antall forsøk en bruker har brukt slik at bruker ikke kan gjette for mange ganger på en kode
    /// </summary>
    /// <param name="userId">Brukerens ID</param>
    /// <param name="userOtpCode">OTP-koden brukeren har oppgitt</param>
    /// <param name="ct"></param>
    /// <returns>Result med OTP-koden med Success eller Failure med egen feilmelding</returns>
    Task<Result<OtpCode>> VerifyOtpCodeAsync(Guid userId, string userOtpCode, CancellationToken ct = default);
}
