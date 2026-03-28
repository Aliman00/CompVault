using System.Security.Claims;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authentication.Cookies;
namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Håndterer token-refresh og brukervalidering via cookie-middleware som kjøres på hver forespørsel
/// </summary>
public class CookieValidationEvents(AuthSettings authSettings, IWebHostEnvironment env) 
    : CookieAuthenticationEvents
{   
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        // Vi gjør en sjekk for å sjekke sist gang vi validerte at brukeren har gydlig refresh token.
        // En egenskap som vi alltid setter etter vi har refreshet token
        string? lastCheckedRaw = context.Properties.GetParameter<string>("LastValidated");
        if (lastCheckedRaw != null &&
            DateTimeOffset.TryParse(lastCheckedRaw, out DateTimeOffset lastChecked) &&
            lastChecked > DateTimeOffset.UtcNow.AddMinutes(-authSettings.ValidationIntervalMinutes))
            return;
        
        string? refreshToken = context.HttpContext.GetRefreshTokenCookie();
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        // Oppdaterer LastValidated optimistisk før selve kallet. Hindrer at flere requester som kjører parallelt
        // prøver å refreshe samtidig
        context.Properties.SetParameter("LastValidated", DateTimeOffset.UtcNow.ToString("O"));
        context.ShouldRenew = true;
        
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
                    // Bruker er deaktivert — logget ut
                    context.RejectPrincipal();
                    return;
                }
                
                return;
            }
            
            TokenResponse? tokenResponse = await refreshTokenResponse.Content
                .ReadFromJsonAsync<TokenResponse>(context.HttpContext.RequestAborted);

            if (tokenResponse == null)
                return;
            
            // Er Identity null og ikke et ClaimsIdentity-objekt, brukeren er ikke autentisert lenger
            if (context.Principal?.Identity is not ClaimsIdentity identity)
            {
                context.RejectPrincipal();
                return;
            }
            
            // Bytter ut gammel claim med ny
            Claim? oldClaim = identity.FindFirst("access_token");
            if (oldClaim != null) 
                identity.RemoveClaim(oldClaim);
            identity.AddClaim(new Claim("access_token", tokenResponse.AccessToken));
            
            context.HttpContext.AppendRefreshTokenCookie(tokenResponse.RefreshToken, authSettings, env);
        }
        catch (Exception)
        {
            // Feiler åpent hvis backend er nede
        }
    }
}