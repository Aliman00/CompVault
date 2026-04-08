using System.Security.Claims;

namespace CompVault.Frontend.Common.Services;

/// <summary>
/// Lagrer den autoriserte brukeren for den aktive kretsen slik at vi kan hente ut claims vi trenger
/// </summary>
public class CircuitUserContext
{
    public ClaimsPrincipal User { get; private set; } = new(new ClaimsIdentity());
    public string? RefreshToken { get; private set; }

    /// <summary>
    /// Setter brukeren og refresh token for aktiv krets. Kalles fra App.razor under SSR
    /// </summary>
    /// <param name="user">Den autentiserte brukeren hentet fra HttpContext</param>
    /// <param name="refreshToken">Refresh token hentet fra request-cookien under SSR</param>
    public void SetUser(ClaimsPrincipal user, string refreshToken)
    {
        User = user;
        RefreshToken = refreshToken;
    }

    /// <summary>
    /// Oppdaterer refresh token etter vellykket token-refresh inne i aktiv krets
    /// </summary>
    /// <param name="refreshToken">Nytt refresh token fra backend</param>
    public void UpdateRefreshToken(string refreshToken) => RefreshToken = refreshToken;
}