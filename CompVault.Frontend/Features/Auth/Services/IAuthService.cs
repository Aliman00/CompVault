using System.Security.Claims;

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
    /// Verifiserer at brukerens kode stemmer. Henter ut claims og lager en ClaimsPrincipal som setter en cookie
    /// i Login Razor-siden
    /// </summary>
    Task<Result<ClaimsPrincipal>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct);

    /// <summary>
    /// Logger brukeren ut av frontend og revoker token i backend. Logger brukeren ut uansett
    /// </summary>
    Task LogOutAsync(CancellationToken ct);
}
