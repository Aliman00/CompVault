using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Features.Auth.Services;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CompVault.Frontend.Pages;

public class LoginCallback(IAuthService authService, AuthSettings authSettings) : PageModel
{
    [BindProperty] public VerifyOtpRequest Request { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return LocalRedirect("/login?error=invalid");

        Result<(ClaimsPrincipal Principal, RefreshTokenResponse Tokens)> result =
            await authService.VerifyOtpAsync(Request, CancellationToken.None);

        if (result.IsFailure)
            return LocalRedirect("/login?error=invalid");

        var (principal, tokens) = result.Value;

        // Access token lagres som claim i auth-cookien
        // AccessTokenHandler leser dette claimet og setter Bearer header på API-kall mot backend
        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim("access_token", tokens.AccessToken));

        // RefreshToken settes som HttpOnly cookie i nettleseren.
        // OnValidatePrincipal leser denne og bruker den til å hente nytt access token slik vi spesifisderer i
        // AuthSettings
        HttpContext.Response.Cookies.Append("refreshToken", tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(authSettings.CookieExpireDays),
            IsEssential = true
        });

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return LocalRedirect("/");
    }
}