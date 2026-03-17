using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Håndterer generering og validering av JWT-tokens.
/// </summary>
public interface IJwtService
{
    /// <summary>Lager et signert JWT access token for brukeren med rollene sine.</summary>
    /// <param name="user">Den innloggede brukeren.</param>
    /// <param name="roles">Rollene brukeren har.</param>
    /// <returns>En JWT-streng.</returns>
    string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);

    /// <summary>
    /// Validerer et (evt. utgått) access token og returnerer claims fra det.
    /// Brukes i refresh-flyten for å hente ut bruker-ID fra det gamle tokenet.
    /// </summary>
    System.Security.Claims.ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
