using System.Security.Claims;
using CompVault.Shared.Constants;
using Microsoft.IdentityModel.JsonWebTokens;
namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Klasse som synkroniserer claims slik at oppdatering av navn, epost, roller etc. oppdateres i sanntid
/// </summary>
internal static class ClaimsSynchronizer
{
    /// <summary>
    /// Hvilke claims vi skal oppdatere. F.eks. JTI trenger vi ikke
    /// </summary>
    private static readonly string[] ClaimsToSync =
    [
        ClaimTypes.Email, 
        "firstName",
        "lastName",
        ClaimTypes.Role,
        Permissions.ClaimType    
    ];
    
    /// <summary>
    /// Fjerner og legger til ny claims ved nytt token tilfelle noe er forandret
    /// </summary>
    /// <param name="identity">ClaimsIdentity</param>
    /// <param name="newAccessToken">Nytt access token fra RefreshToken-endepunktet</param>
    internal static void RefreshClaimsFromAccessToken(ClaimsIdentity identity, string newAccessToken)
    {
        // Brukern en handler for å parse token
        var handler = new JsonWebTokenHandler();
        if (!handler.CanReadToken(newAccessToken))
            return;
        
        // Parser token for å hente ut claims
        JsonWebToken jwtToken = handler.ReadJsonWebToken(newAccessToken);
        
        // Iterer over alle relevante claims og fjerner gamle og legger til ny
        foreach (string claimType in ClaimsToSync)
        {
            var oldClaims = identity.FindAll(claimType).ToList();
            
            foreach (Claim claim in oldClaims)
            {
                identity.RemoveClaim(claim);
            }

            foreach (Claim claim in jwtToken.Claims.Where(c => c.Type == claimType))
            {
                identity.AddClaim(new Claim(claimType, claim.Value));
            }
        }

    }
}