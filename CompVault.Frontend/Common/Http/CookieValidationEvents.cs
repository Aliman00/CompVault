using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http.Models;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Håndterer token-refresh og brukervalidering via cookie-middleware som kjøres på hver forespørsel
/// </summary>
public class CookieValidationEvents(
    AuthSettings authSettings, 
    ILogger<CookieValidationEvents> logger,
    ITokenRefreshService tokenRefreshService) 
    : CookieAuthenticationEvents
{   
    
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {   
        // Henter UserId fra claimen
        string? userId = context.Principal?.FindFirst("sub")?.Value;
        if (userId == null)
        {
            logger.LogWarning("Ingen innlogget autentisert bruker - logges ut");
            await RejectAndSignOutAsync(context);
            return;
        }
        
        Result<RefreshRecord> result = await tokenRefreshService.RefreshPairAsync(userId, context.HttpContext, 
            context.HttpContext.RequestAborted);
        
        if (result.IsFailure)
        {
            // NotFound betyyr ingen refresh token i cookie og Unathorized betyr at brukeren er deaktivert
            // Begge logger brukeren ut
            if (result.Error?.Code == ErrorCode.NotFound || result.Error?.Code == ErrorCode.Unauthorized)
                await RejectAndSignOutAsync(context);
            return;
        }
        
        ApplyRefreshResult(context, result.Value!);
    }
    
    
    // Oppdaterer hver context med begge tokens - de overskriver hverandre hvis de sendes på forskjellige tidspunkt
    private void ApplyRefreshResult(CookieValidatePrincipalContext context, RefreshRecord refreshRecord)
    {
        tokenRefreshService.ApplyTokenPair(context.HttpContext, refreshRecord);
        context.ShouldRenew = true;
    }
    
    
    // Logger brukeren ut ved å rejecte Principal, logge oss ut fra HttpContext (som igjen sletter auth-cookie)
    // og manuelt slette refresh token-cookie hvis den eksisterer
    private static async Task RejectAndSignOutAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.HttpContext.Response.Cookies.Delete("refreshToken");
    }
}