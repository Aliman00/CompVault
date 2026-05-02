using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.DTOs.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace CompVault.Frontend.Common.Pages;

/// <summary>
/// Modellen til en SSR-side som lar oss legge til token i nettlesere - JS-scriptet submitter som gjør at
/// OnPostAsync blir kjørt
/// </summary>
public class LoginCallback(AuthSettings authSettings, IWebHostEnvironment env) : PageModel
{
    [BindProperty]
    public TokenResponse TokenResponse { get; set; } = new();

    /// <summary>
    /// Sender VerifyOtpRequest til backend og legger til en cookie i nettleseren.
    /// Redirecter til Login-siden hvis noe failer 
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(TokenResponse.AccessToken) ||
            string.IsNullOrWhiteSpace(TokenResponse.RefreshToken))
            return LocalRedirect("/?error=invalid");

        IEnumerable<Claim> claims = JwtExtensions.ParseClaimsFromJwt(TokenResponse.AccessToken);
        var identity = new ClaimsIdentity(claims, "jwt");
        identity.AddClaim(new Claim("access_token", TokenResponse.AccessToken));
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        HttpContext.AppendRefreshTokenCookie(TokenResponse.RefreshToken, authSettings, env);

        return LocalRedirect("/");
    }
}