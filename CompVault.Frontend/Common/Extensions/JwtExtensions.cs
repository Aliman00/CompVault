using System.Security.Claims;
using System.Text.Json;

namespace CompVault.Frontend.Common.Extensions;

public static class JwtExtensions
{   
    /// <summary>
    /// Parser claims fra JWT for å legge cookies inn i nettleseren
    /// </summary>
    /// <param name="jwt"></param>
    /// <returns></returns>
    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
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