using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Dev;

public interface IDevService
{
    /// <summary>
    /// Verifiserer at brukerens kode stemmer. Legger til tokens, claims og  setter brukeren som innlogget ved suksess
    /// </summary>
    Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct);
}