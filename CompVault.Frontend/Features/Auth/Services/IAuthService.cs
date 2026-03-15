using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity.Data;

namespace CompVault.Frontend.Features.Auth.Services;

public interface IAuthService
{
    /// <summary>
    /// API-kall med LoginRequest for å genere en OTP-kode til brukeren som skal logge inn. Frontend
    /// navigerer til neste side, mens backend oppretter en OTP-kode og sender utifra ønsket leveringsmetode lagt med
    /// i LoginResponse
    /// </summary>
    /// <param name="request">Email og ønsket delivery method</param>
    /// <param name="ct"></param>
    /// <returns>Result med Success eller failure hvis noe gikk galt</returns>
    Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct);
}