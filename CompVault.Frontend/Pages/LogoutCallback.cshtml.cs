using CompVault.Frontend.Common.Constants;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CompVault.Frontend.Pages;

public class LogoutCallback : PageModel
{   
    /// <summary>
    /// Logger brukeren ut ved å slette auth-cookie i nettleseren og redirecte tilbake til login-siden igjen
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Response.Cookies.Delete("refreshToken");
        return LocalRedirect(PageRoutes.Auth.LoginEmail);
    }
}