using System.Security.Claims;

using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Common.Services;

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Oppdaterer CircuitUserContext hver gang kretsen kobler til — inkludert reconnect
/// Dette sikrer at brukeren alltid er tilgjengelig inne i kretsen selv om HttpContext er null
/// </summary>
internal sealed class CircuitUserContextHandler(
    CircuitUserContext circuitUserContext,
    IHttpContextAccessor httpContextAccessor)
    : CircuitHandler
{
    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken ct)
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        
        if (user?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        string? refreshToken = httpContextAccessor.HttpContext?.GetRefreshTokenCookie()
                               ?? circuitUserContext.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
            return Task.CompletedTask;

        circuitUserContext.SetUser(user, refreshToken);
        return Task.CompletedTask;
    }
}