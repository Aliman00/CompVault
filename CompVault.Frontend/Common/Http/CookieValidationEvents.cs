using System.Security.Claims;
using CompVault.Frontend.Common.Configuration;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Håndterer token-refresh og brukervalidering via cookie-middleware
/// </summary>
public class CookieValidationEvents(AuthSettings settings, IWebHostEnvironment env) 
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        string? lastCheckedRaw = context.Properties.GetParameter<string>("LastValidated");
        if (lastCheckedRaw != null &&
            DateTimeOffset.TryParse(lastCheckedRaw, out DateTimeOffset lastChecked) &&
            lastChecked > DateTimeOffset.UtcNow.AddMinutes(-settings.ValidationIntervalMinutes))
        {
            return;
        }

        string? refreshToken = context.HttpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
        {
            context.RejectPrincipal();
            return;
        }

        // Sett LastValidated OPTIMISTISK FØR refresh-kallet
        // Dette hindrer at parallelle requests alle prøver å refreshe samtidig
        context.Properties.SetParameter("LastValidated", DateTimeOffset.UtcNow.ToString("O"));
        context.ShouldRenew = true;

        HttpClient client = context.HttpContext.RequestServices
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(BackendApiSettings.AuthClientName);

        try
        {
            var request = new RefreshTokenRequest { RefreshToken = refreshToken };
            HttpResponseMessage response = await client.PostAsJsonAsync(
                ApiRoutes.Auth.RefreshFull, request, context.HttpContext.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                // Kun kast brukeren ut hvis tokenet er faktisk ugyldig — ikke ved race condition
                // Vi kan ikke skille disse to tilfellene sikkert, så vi bruker fail open
                return;
            }

            RefreshTokenResponse? tokenResponse = await response.Content
                .ReadFromJsonAsync<RefreshTokenResponse>(context.HttpContext.RequestAborted);

            if (tokenResponse is null)
                return;

            var identity = (ClaimsIdentity)context.Principal!.Identity!;
            var gammeltClaim = identity.FindFirst("access_token");
            if (gammeltClaim is not null) identity.RemoveClaim(gammeltClaim);
            identity.AddClaim(new Claim("access_token", tokenResponse.AccessToken));

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
            // Fail open — backend er nede
            return;
        }
    }
}