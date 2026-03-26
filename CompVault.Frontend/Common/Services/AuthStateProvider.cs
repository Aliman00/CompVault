using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Components.Authorization;

namespace CompVault.Frontend.Common.Services;

/// <summary>
/// Lagrer og distribuerer autentiseringstilstanden for en krets.
/// Komponenter som sjekker autentisering kaller på denne servicen
/// </summary>
/// <param name="tokenProvider"></param>
public class AuthStateProvider(TokenProvider tokenProvider) : AuthenticationStateProvider
{
    // Nåværende bruker er satt som ikke-autentisert ved opprettelse
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());
    
    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_currentUser));
    
    /// <summary>
    /// Lagrer tokens til en bruker i en krets, og oppdaterer AuthenticationState til innlogget
    /// </summary>
    public void MarkUserAsAuthenticated(string accessToken)
    {
        tokenProvider.AccessToken = accessToken;
        
        // Henter CLaimene fra AccessToken og lagrer det til innlogget bruker
        IEnumerable<Claim> claims = ParseClaimsFromJwt(accessToken);
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        
        // Oppdaterer alle AuthorizeView-komponenter - da vil vi se i komponenter at brukeren er innogget
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
    
    /// <summary>
    /// Fjerner tokens, setter brukeren som ikke-autentisert og oppdaterer komponentenel
    /// </summary>
    public void MarkUserAsLoggedOut()
    {
        tokenProvider.AccessToken = null;
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    /// <summary>
    /// Oppdaterer accessToken i minnet etter vellykket token-refresh
    /// </summary>
    public void UpdateAccessToken(string newAccessToken) => tokenProvider.AccessToken = newAccessToken;
    

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        // JWT er 3 stk base64url-segmenter som separeres med en punkt - vi skal ha midterste som blir kalt payload
        string base64UrlPayload = jwt.Split('.')[1];
        
        // Konverterer til vanlig base64 og fjerner padding som base64url fjernet
        // Vi må gjøre dette for at Conver.FromBase64String skal kunne det om til json
        string standardBase64 = base64UrlPayload.Replace('-', '+').Replace('_', '/');
        string paddedBase64 = standardBase64.PadRight(standardBase64.Length + 
                                                      (4 - standardBase64.Length % 4) % 4, '=');
        // Base64 gjøres om til JSON for å kunne deserialiseres, slik at vi får en ordbok med verdiene fra JWT-en
        string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(paddedBase64));
        Dictionary<string, JsonElement>? parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        
        // Vi returnerer en liste med Claims, hentet fra nøkkelparet i ordboka. Vi har lagt til at vi håndterer claims
        // som er arrays (feks Roller)
        var claims = new List<Claim>();
        foreach (KeyValuePair<string, JsonElement> kv in parsed)
        {
            if (kv.Value.ValueKind == JsonValueKind.Array)
            {
                claims.AddRange(kv.Value.EnumerateArray()
                    .Select(element => new Claim(kv.Key, element.ToString())));
            }
            else
            {
                claims.Add(new Claim(kv.Key, kv.Value.ToString()));
            }
        }
        
        return claims;
    }
}