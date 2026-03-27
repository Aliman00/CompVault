using System.Security.Claims;

using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;

using Microsoft.AspNetCore.Authentication.Cookies;

namespace CompVault.Frontend.Common.Configuration;

/// <summary>
/// Håndterer token-refresh og brukervalidering via cookie-middleware
/// </summary>
public class CookieValidationEvents(AuthSettings settings, IWebHostEnvironment env) 
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        // Sjekk kun hvert N. minutt — ikke på hvert SignalR-signal
        string? lastCheckedRaw = context.Properties.GetParameter<string>("LastValidated");
        if (lastCheckedRaw != null &&
            DateTimeOffset.TryParse(lastCheckedRaw, out DateTimeOffset lastChecked) &&
            lastChecked > DateTimeOffset.UtcNow.AddMinutes(-settings.ValidationIntervalMinutes))
        {
            return;
        }

        // refreshToken-cookien er tilgjengelig her fordi OnValidatePrincipal
        // kjøres på ekte HTTP-requests fra nettleseren, ikke SignalR
        string? refreshToken = context.HttpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        // Anonymklient — ingen Bearer header, unngår rekursjon med AccessTokenHandler
        HttpClient client = context.HttpContext.RequestServices
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(BackendApiSettings.AuthClientName);

        // HttpClient er server-til-server — cookien må forwardes manuelt
        client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");

        try
        {
            HttpResponseMessage response = await client.PostAsync(
                ApiRoutes.Auth.RefreshFull, null,
                context.HttpContext.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                context.RejectPrincipal();
                context.HttpContext.Response.Cookies.Delete("refreshToken");
                return;
            }

            RefreshTokenResponse? tokenResponse = await response.Content
                .ReadFromJsonAsync<RefreshTokenResponse>(context.HttpContext.RequestAborted);

            if (tokenResponse is null)
            {
                context.RejectPrincipal();
                context.HttpContext.Response.Cookies.Delete("refreshToken");
                return;
            }

            // Oppdater access_token-claimet med det nye tokenet
            var identity = (ClaimsIdentity)context.Principal!.Identity!;
            var gammeltClaim = identity.FindFirst("access_token");
            if (gammeltClaim is not null) identity.RemoveClaim(gammeltClaim);
            identity.AddClaim(new Claim("access_token", tokenResponse.AccessToken));

            // Oppdater refreshToken-cookien med det roterte tokenet
            context.HttpContext.Response.Cookies.Append("refreshToken",
                tokenResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !env.IsDevelopment(),
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(settings.CookieExpireDays),
                    IsEssential = true
                });
        }
        catch (Exception)
        {
            // Fail open — la brukeren vøre pålogget ved midlertidig backend-nedetid
            return;
        }

        context.Properties.SetParameter("LastValidated", DateTimeOffset.UtcNow.ToString("O"));
        context.ShouldRenew = true;
    }
}