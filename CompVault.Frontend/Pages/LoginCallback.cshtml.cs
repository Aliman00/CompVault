using System.Security.Claims;

using CompVault.Frontend.Features.Auth.Services;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CompVault.Frontend.Pages;

public class LoginCallback(IAuthService authService) : PageModel
{
    [BindProperty] public VerifyOtpRequest Request { get; set; } = new();
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return LocalRedirect("/login?error=invalid");

        Result<ClaimsPrincipal> result = await authService.VerifyOtpAsync(Request, CancellationToken.None);

        if (result.IsFailure)
            return LocalRedirect("/login?error=invalid");

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Value!);

        return LocalRedirect("/");
    }
}