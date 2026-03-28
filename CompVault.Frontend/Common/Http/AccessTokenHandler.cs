using System.Security.Claims;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Legger til Bearer header på alle server-til-server kall mot backend.
/// Leser access token fra auth-cookie claims — oppdateres automatisk av OnValidatePrincipal.
/// </summary>
public class AccessTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment env,
    AuthSettings authSettings) : DelegatingHandler
{   
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Kloner forespørselen før den blir konsumert i sending
        HttpRequestMessage clonedRequest = await CloneAsync(request);
        
        SetAuthHeader(request);
        HttpResponseMessage response = await base.SendAsync(request, ct);
        
        // Får vi Unathorized her, token er utgått
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;
        
        // Bruker refresh fra cookie til å autoriseres på nytt - failer det så sender vi koden videre til kalleren
        // og hvis errorkoden er AccountInactive, så er brukeren utlogget
        Result  refreshResult  = await TryRefreshAsync(ct);
        if (refreshResult.IsFailure && 
            refreshResult.Error?.Code == ErrorCode.AccountInactive)
            return response;
        
        SetAuthHeader(clonedRequest);
        return await base.SendAsync(clonedRequest, ct);
    }
    
    // Setter en Bearer header
    private void SetAuthHeader(HttpRequestMessage request)
    {
        string? accessToken = httpContextAccessor.HttpContext?.User.FindFirst("access_token")?.Value;

        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
    
    
    private async Task<Result> TryRefreshAsync(CancellationToken ct)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) 
            return Result.Failure(AppError.Create(ErrorCode.Unknown, "Ingen HttpContext."));
        
        string? refreshToken = httpContext.GetRefreshTokenCookie();
        if (string.IsNullOrEmpty(refreshToken))
            return Result.Failure(AppError.Create(ErrorCode.InvalidToken, "Ingen refresh token i cookie."));
        
        // Oppreter en klient som ikke har AccessTokenHandler påkoblet
        HttpClient client = httpClientFactory.CreateClient(BackendApiSettings.AuthClientName);

        try
        {
            // En request for å fornye tokens
            var request = new RefreshTokenRequest { RefreshToken = refreshToken };
            HttpResponseMessage response = await client.PostAsJsonAsync(ApiRoutes.Auth.RefreshFull, request, 
                ct);

            if (!response.IsSuccessStatusCode)
            {
                ProblemDetail? problem = await response.Content
                    .ReadFromJsonAsync<ProblemDetail>(ct);

                ErrorCode code = Enum.TryParse(problem?.Code, out ErrorCode parsed)
                    ? parsed
                    : ErrorCode.Unknown;

                return Result.Failure(AppError.Create(code, problem?.Message ?? string.Empty));
            }
            
            TokenResponse? tokenResponse = await response.Content
                .ReadFromJsonAsync<TokenResponse>(ct);
            if (tokenResponse == null) 
                return Result.Failure(AppError.Create(ErrorCode.Unknown, "Tom respons fra backend."));
            
            // Sjekker at ClaimsIdentity ikke er null og at det er riktig tyype
            if (httpContext.User.Identity is not ClaimsIdentity identity)
                return Result.Failure(AppError.Create(ErrorCode.Unauthorized, "Ingen ClaimsIdentity."));
            
            // Bytter ut gammel claim med ny
            Claim? oldClaim = identity.FindFirst("access_token");
            if (oldClaim != null) 
                identity.RemoveClaim(oldClaim);
            identity.AddClaim(new Claim("access_token", tokenResponse.AccessToken));
            
            httpContext.AppendRefreshTokenCookie(tokenResponse.RefreshToken, authSettings, env);

            return Result.Success();
        }
        catch
        {
            return Result.Failure(AppError.Create(ErrorCode.Unknown, "Uventet feil ved token-refresh."));
        }
    }
    
    // Kloner foprespørselen siden den er single-use og blir konsumert
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (KeyValuePair<string, IEnumerable<string>> header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        // Hvis forespørselen har en body med innhold så kloner vi den og
        if (original.Content is null) 
            return clone;

        byte[] body = await original.Content.ReadAsByteArrayAsync();
        clone.Content = new ByteArrayContent(body);
        foreach (KeyValuePair<string, IEnumerable<string>> header in original.Content.Headers)
        {
            clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}