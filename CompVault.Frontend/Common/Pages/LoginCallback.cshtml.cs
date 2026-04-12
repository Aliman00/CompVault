using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Features.Auth.Services;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CompVault.Frontend.Common.Pages;

/// <summary>
/// Modellen til en SSR-side som lar oss legge til token i nettlesere - JS-scriptet submitter som gjør at
/// OnPostAsync blir kjørt
/// </summary>
public class LoginCallback(IAuthService authService, AuthSettings authSettings, IWebHostEnvironment env) : PageModel
{
    [BindProperty]
    public VerifyOtpRequest OtpRequest { get; set; } = new();

    /// <summary>
    /// Sender VerifyOtpRequest til backend og legger til en cookie i nettleseren.
    /// Redirecter til Login-siden hvis noe failer TODO: Må implementeres i Login-page
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return LocalRedirect("/login?error=invalid");

        Result<(ClaimsPrincipal Principal, TokenResponse Tokens)> verifyOtpResult =
            await authService.VerifyOtpAsync(OtpRequest, HttpContext.RequestAborted);

        if (verifyOtpResult.IsFailure)
            return LocalRedirect("/login?error=invalid");

        (ClaimsPrincipal? principal, TokenResponse? tokens) = verifyOtpResult.Value;

        if (principal.Identity is not ClaimsIdentity identity)
            return LocalRedirect("/login?error=invalid");

        // Setter opp auth-cookie først, deretter RefreshToken-cookie
        identity.AddClaim(new Claim("access_token", tokens.AccessToken));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        HttpContext.AppendRefreshTokenCookie(tokens.RefreshToken, authSettings, env);

        return LocalRedirect("/");
    }
}