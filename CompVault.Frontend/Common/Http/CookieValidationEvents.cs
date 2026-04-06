using System.Collections.Concurrent;
using System.Security.Claims;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Håndterer token-refresh og brukervalidering via cookie-middleware som kjøres på hver forespørsel
/// </summary>
public class CookieValidationEvents(
    AuthSettings authSettings, 
    IWebHostEnvironment env, 
    ILogger<CookieValidationEvents> logger) 
    : CookieAuthenticationEvents
{   
    // En ordbok som sjekker om et kall holder på å refreshe token, og en ordbok som lar oss sjekke om vi har
    // refreshed nylig. Sammen brukes de for å sikre ingen paralelle kall til backend
    private static readonly ConcurrentDictionary<string, AsyncLazy<bool>> PendingRefreshes = new();
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastRefreshed = new();

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {   
        // Sjekker først om LastValidated-claimen er nylig oppdatert
        if (IsRecentlyValidated(context))
            return;
        
        // Henter UserId fra claimen
        string? userId = context.Principal?.FindFirst("sub")?.Value;
        if (userId == null)
        {
            logger.LogWarning("Ingen innlogget autentisert bruker - logges ut");
            await RejectAndSignOutAsync(context);
            return;
        }
        
        // Sjekker om det finnes en eksisterende nøkkel med med denne bruker ID-en.
        // Dette sikrer at parallelle requester bruker samme RefreshAsync-instance.
        // Lazy sikrer at den ikke kjører før vi kaller den selv
        AsyncLazy<bool> pendingRefresh = PendingRefreshes.GetOrAdd(userId,
            _ => new AsyncLazy<bool>(() => RefreshAsync(context, userId)));
        
        try
        {
            await pendingRefresh;
        }
        finally
        {
            // Vi fjerner brukerne fra PendingRefreshes til slutt
            PendingRefreshes.TryRemove(userId, out _);
            
            // Begge parallelle requests leser LastRefreshed etter fornying av token.
            // Verdien fjernes aldri, bare overskrives. Og begge setter LastValidated i sin egen context
            if (LastRefreshed.TryGetValue(userId, out DateTimeOffset refreshedAt))
            {
                context.Properties.SetParameter("LastValidated", refreshedAt.ToString("O"));
                context.ShouldRenew = true;
            }
        }
    }
    
    /// <summary>
    /// Refreshen Token - sender API-kall til backend og oppdaterer tokens og context
    /// </summary>
    private async Task<bool> RefreshAsync(CookieValidatePrincipalContext context, string userId)
    {
        string? refreshToken = context.HttpContext.GetRefreshTokenCookie();
        if (string.IsNullOrEmpty(refreshToken))
        {
            
            logger.LogWarning("Ingen refresh token funnet - logger brukeren ut");
            await RejectAndSignOutAsync(context);
            return false;
        }
        
        // Oppretter en klient med AuthClienten uten påkoblet AccessTokenHandler
        HttpClient client = context.HttpContext.RequestServices
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(BackendApiSettings.AuthClientName);

        try
        {
            var refreshTokenRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
            HttpResponseMessage refreshTokenResponse = await client.PostAsJsonAsync(
                ApiRoutes.Auth.RefreshFull, refreshTokenRequest, context.HttpContext.RequestAborted);
            
            // Sjekker om errorkoden er AccountInactive, det setter brukeren som utlogget.
            // Hvis feks race condition - to requester kjører nesten paralellelt så sender backend InvalidToken.
            // Vi lar den faile åpent, i og med at en av forespørslene kan ha refreshet token
            if (!refreshTokenResponse.IsSuccessStatusCode)
            {
                ProblemDetail? problem = await refreshTokenResponse.Content
                    .ReadFromJsonAsync<ProblemDetail>(context.HttpContext.RequestAborted);

                if (problem?.Code == nameof(ErrorCode.AccountInactive))
                {
                    // Bruker er deaktivert — logget ut både fra context og sletter token
                    logger.LogWarning("Bruker er deaktivert — logger brukeren ut");
                    await RejectAndSignOutAsync(context);
                    return false;
                }
                
                // Backend kan returnere annen feil f.eks. ved server nede. Forblir innlogget, og AccessTokenHandler
                // håndterer 401 på neste kall til backend
                logger.LogDebug("Token-refresh feilet med {Code}", problem?.Code);
                return false;
            }
            
            TokenResponse? tokenResponse = await refreshTokenResponse.Content
                .ReadFromJsonAsync<TokenResponse>(context.HttpContext.RequestAborted);

            if (tokenResponse == null)
            {
                logger.LogWarning("Tom respons fra backend ved token-refresh — feiler åpent");
                return false;
            }
            
            // Principal og Identity er garantert satt av cookie-middlewaren
            var identity = (ClaimsIdentity)context.Principal!.Identity!;
            
            // Bytter ut gammel claim med ny
            Claim? oldClaim = identity.FindFirst("access_token");
            if (oldClaim != null) 
                identity.RemoveClaim(oldClaim);
            identity.AddClaim(new Claim("access_token", tokenResponse.AccessToken));
            
            context.HttpContext.AppendRefreshTokenCookie(tokenResponse.RefreshToken, authSettings, env);
            
            // Lagrer tidspunktet så parallelle requests kan oppdatere egen context
            LastRefreshed[userId] = DateTimeOffset.UtcNow;
            
            context.Properties.SetParameter("LastValidated", DateTimeOffset.UtcNow.ToString("O"));
            context.ShouldRenew = true;
            
            logger.LogDebug("Token refreshet og principal oppdatert");

            return true;
        }
        catch (Exception ex)
        {
            // Feiler åpent hvis backend er nede
            logger.LogError(ex, "Uventet feil ved token-refresh i CookieValidationEvents");
            return false;
        }
    }

    
    // Vi sjekker sist gang vi validerte at brukeren hadde gydlig refresh token.
    // En egenskap som vi alltid setter etter vi har refreshet token
    private bool IsRecentlyValidated(CookieValidatePrincipalContext context)
    {
        string? lastCheckedRaw = context.Properties.GetParameter<string>("LastValidated");
        return lastCheckedRaw != null &&
            DateTimeOffset.TryParse(lastCheckedRaw, out DateTimeOffset lastChecked) &&
            lastChecked > DateTimeOffset.UtcNow.AddMinutes(-authSettings.ValidationIntervalMinutes);
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