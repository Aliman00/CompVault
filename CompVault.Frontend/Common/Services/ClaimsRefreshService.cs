using System.Security.Claims;

using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Http.Models;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Components.Authorization;

namespace CompVault.Frontend.Common.Services;

public class ClaimsRefreshService(
    ITokenRefreshService tokenRefreshService,
    CircuitUserContext circuitUserContext,
    AuthenticationStateProvider authStateProvider,
    ILogger<ClaimsRefreshService> logger) : IClaimsRefreshService
{
    /// <inheritdoc />
    public async Task RefreshTokensAsync()
    {   
        string? userId = circuitUserContext.User.FindFirst("sub")?.Value;
        string? refreshToken = circuitUserContext.RefreshToken;
        
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("Feil ved manuell opdatering av token — mangler userId fra CircuiUserContext");
            return;
        }
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("Feil ved manuell opdatering av token — mangler refreshToken fra CircuiUserContext");
            return;
        }
        
        // Invaliderer cooldown slik at kallet vårt går igjennom og oppdaterer token
        tokenRefreshService.InvalidateCooldown(userId);
        
        Result<RefreshRecord> result = await tokenRefreshService.RefreshPairAsync(userId, refreshToken);
        if (result.IsFailure)
        {
            logger.LogWarning("Manuel refresh token feilet med {Code}", result.Error?.Code);
            return;
        }
        
        circuitUserContext.UpdateRefreshToken(result.Value!.RefreshToken);
        
        // Oppdater claims i ClaimsIdentity
        if (circuitUserContext.User.Identity is ClaimsIdentity identity)
        {
            Claim? gammeltToken = identity.FindFirst("access_token");
            if (gammeltToken != null)
                identity.RemoveClaim(gammeltToken);
            identity.AddClaim(new Claim("access_token", result.Value!.AccessToken));

            ClaimsSynchronizer.RefreshClaimsFromAccessToken(identity, result.Value!.AccessToken);
            
            // Fortell Blazor at auth-state er endret — komponenter re-rendrer
            ((AuthStateProvider)authStateProvider).NotifyStateChanged();

            logger.LogDebug("Manuell refresh av token vellykket");
            
            // Refresher token igjen på neste navigering/refresh slik at CookieValidationEvent får oppdatert cookies
            tokenRefreshService.InvalidateCooldown(userId);
        }
    }
}