using CompVault.Frontend.Common.Configuration;

namespace CompVault.Frontend.Common.Extensions;

/// <summary>
/// Extension-metoder på HttpContext - setting/henting av cookies
/// </summary>
public static class HttpContextExtensions
{
    private const string RefreshTokenCookieName = "refreshToken";

    /// <summary>
    /// Setter refresh token-cookie med riktig konfigurasjon.
    /// Brukes ved innlogging, token-refresh i AccessTokenHandler og CookieValidationEvents
    /// </summary>
    public static void AppendRefreshTokenCookie(this HttpContext httpContext, string refreshToken,
        AuthSettings settings, IWebHostEnvironment env)
    {
        httpContext.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(settings.CookieExpireDays),
            IsEssential = true
        });
    }

    /// <summary>
    /// Henter refreshToken fra request-cookien.
    /// </summary>
    public static string? GetRefreshTokenCookie(this HttpContext httpContext) =>
        httpContext.Request.Cookies[RefreshTokenCookieName];
}