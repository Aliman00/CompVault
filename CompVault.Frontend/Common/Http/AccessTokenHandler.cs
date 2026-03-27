using CompVault.Frontend.Common.Configuration;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;

namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Legger til Bearer header på alle server-til-server kall mot backend.
/// Leser access token fra auth-cookie claims — oppdateres automatisk av OnValidatePrincipal.
/// </summary>
public class AccessTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        SetAuthHeader(request);
        HttpResponseMessage response = await base.SendAsync(request, ct);

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        // Token er utgått — forsøk stille refresh
        bool refreshed = await TryRefreshAsync(ct);
        if (!refreshed)
            return response; // Sender 401 videre

        // Prøv på nytt med nytt token
        var retry = await CloneAsync(request);
        SetAuthHeader(retry);
        return await base.SendAsync(retry, ct);
    }

    private void SetAuthHeader(HttpRequestMessage request)
    {
        string? accessToken = httpContextAccessor.HttpContext?.User
            .FindFirst("access_token")?.Value;

        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return false;

        string? refreshToken = httpContext.Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken)) return false;

        var client = httpClientFactory.CreateClient(BackendApiSettings.AuthClientName);
        client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={refreshToken}");

        try
        {
            var response = await client.PostAsync(
                ApiRoutes.Auth.RefreshFull, null, ct);

            if (!response.IsSuccessStatusCode) return false;

            var tokenResponse = await response.Content
                .ReadFromJsonAsync<RefreshTokenResponse>(ct);

            if (tokenResponse is null) return false;

            // Oppdater claimet i HttpContext.User direkte
            // slik at neste SetAuthHeader-kall bruker det nye tokenet
            var identity = (System.Security.Claims.ClaimsIdentity)httpContext.User.Identity!;
            var gammelt = identity.FindFirst("access_token");
            if (gammelt is not null) identity.RemoveClaim(gammelt);
            identity.AddClaim(new System.Security.Claims.Claim("access_token", tokenResponse.AccessToken));

            // Oppdater refreshToken-cookien i responsen
            httpContext.Response.Cookies.Append("refreshToken", tokenResponse.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true
                });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content is null) 
            return clone;

        byte[] body = await original.Content.ReadAsByteArrayAsync();
        clone.Content = new ByteArrayContent(body);
        foreach (var header in original.Content.Headers)
        {
            clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}