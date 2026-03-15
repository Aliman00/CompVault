using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity.Data;

namespace CompVault.Frontend.Features.Auth.Services;

public interface IAuthService
{
    /// <summary>
    /// API-kall med LoginRequest som setter token lokalt og returnerer et Result med <see cref="LoginResponse"/>
    /// </summary>
    /// <param name="request">Email og ønsket delivery method</param>
    /// <param name="ct"></param>
    /// <returns>LoginResponse</returns>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}