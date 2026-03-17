using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Auth.Services;

/// <summary>
/// Alt som har med innlogging og tokens å gjøre havner her.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Steg 1: Sender en engangs-kode (OTP) til brukeren via valgt kanal.
    /// Returnerer alltid suksess for å unngå at angripere kan kartlegge hvilke
    /// e-poster som er registrert i systemet.
    /// Bruker StopWatch for å sikre at metoden bruker like lang tid om brukeren eksisterer eller ikke
    /// </summary>
    Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Steg 2: Verifiserer OTP-koden og utsteder et JWT token-par ved suksess.
    /// Returnerer Failure hvis brukeren ikke eksisterer, feil kode eller ingen aktiv kode
    /// Bruker StopWatch for å sikre at metoden bruker like lang tid om brukeren eksisterer eller ikke
    /// </summary>
    Task<Result<LoginResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);

    /// <summary>
    /// Utsteder et nytt access token ved hjelp av et gyldig refresh token.
    /// </summary>
    Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Ugyldiggjør et refresh token — i praksis logger brukeren ut.
    /// </summary>
    Task<Result> RevokeRefreshTokenAsync(RevokeTokenRequest request, CancellationToken ct = default);
}
