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
    /// Verifiserer OTP-koden. Midlertidig hardkodet for testing.
    /// </summary>
    Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct);

    /// <summary>
    /// Sjekker om brukeren er logget inn
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Logger ut brukeren
    /// </summary>
    void Logout();
}
