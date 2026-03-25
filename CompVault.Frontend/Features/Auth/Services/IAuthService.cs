using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Auth.Services;

public interface IAuthService
{
    /// <summary>
    /// API-kall med RequestOtpRequest for å genere en OTP-kode til brukeren som skal logge inn. Frontend
    /// navigerer til neste side, mens backend oppretter en OTP-kode og sender utifra ønsket leveringsmetode
    /// </summary>
    Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct);
    
    /// <summary>
    /// Verifiserer at brukerens kode stemmer. Legger til tokens, claims og  setter brukeren som innlogget ved suksess
    /// </summary>
    Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct);
}