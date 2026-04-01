using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using CompVault.Shared.Constants;

namespace CompVault.Backend.Infrastructure.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Henter brukerens ID fra JWT-token i headeren, eller kaster en feil hvis noen har tuklet med JWT-en
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        string? userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            throw new UnauthorizedAccessException("Bruker-ID mangler eller er ugyldig i tokenet.");

        return userId;
    }

    /// <summary>
    /// Sjekker om brukeren har en spesifikk permission.
    /// </summary>
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        return user.HasClaim(Permissions.ClaimType, permission);
    }

    /// <summary>
    /// Henter alle permissions fra brukerens claims.
    /// </summary>
    public static IReadOnlyList<string> GetPermissions(this ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToList();
    }
}