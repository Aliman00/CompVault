using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
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
    ILogger<CookieValidationEvents> logger,
    ITokenRefreshService tokenRefreshService,
    AuthSettings authSettings,
    IWebHostEnvironment env)
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

        string? refreshToken = context.HttpContext.GetRefreshTokenCookie();
        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("Ingen refresh token i cookie — logger brukeren ut");
            await RejectAndSignOutAsync(context);
            return;
        }

        Result<RefreshRecord> result = await tokenRefreshService.RefreshPairAsync(userId, refreshToken,
            context.HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            // Nylig refreshet — ikke en feil, bare cooldown. Brukeren forblir innlogget
            if (result.Error?.Code == ErrorCode.RecentlyRefreshed)
                return;

            // NotFound betyr ingen refresh token i cookie og Unathorized betyr at brukeren er deaktivert
            // Begge logger brukeren ut
            if (result.Error?.Code == ErrorCode.NotFound || result.Error?.Code == ErrorCode.Unauthorized)
                await RejectAndSignOutAsync(context);

            // Alle andre feil (Unknown, InternalError, server nede) brukeren forblir innlogget
            return;
        }

        ApplyRefreshResult(context, result.Value!);
    }


    // Skriver nytt access token inn i HttpContext.User så neste retry i samme krets får riktig token
    private void ApplyRefreshResult(CookieValidatePrincipalContext context, RefreshRecord refreshRecord)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return;

        Claim? gammeltToken = identity.FindFirst("access_token");
        if (gammeltToken != null)
            identity.RemoveClaim(gammeltToken);
        identity.AddClaim(new Claim("access_token", refreshRecord.AccessToken));

        context.HttpContext.AppendRefreshTokenCookie(refreshRecord.RefreshToken, authSettings, env);
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