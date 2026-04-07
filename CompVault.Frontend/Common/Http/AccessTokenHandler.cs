using CompVault.Frontend.Common.Http.Models;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Legger til Bearer header på alle server-til-server kall mot backend.
/// Leser access token fra auth-cookie claims — oppdateres automatisk av OnValidatePrincipal
/// </summary>
public class AccessTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AccessTokenHandler> logger,
    ITokenRefreshService tokenRefreshService) : DelegatingHandler
{   
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Kloner forespørselen før den blir konsumert i sending
        HttpRequestMessage clonedRequest = await CloneAsync(request);
        
        SetAuthHeader(request);
        HttpResponseMessage response = await base.SendAsync(request, ct);
        
        // Returnerer responsen så fremt ikke vi får Unauthorized
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;
        
        // Bruker refresh fra cookie til å autoriseres på nytt - failer det så sender vi koden videre til kalleren
        Result refreshResult = await TryRefreshAsync(ct);
        if (refreshResult.IsFailure)
        {
            logger.LogDebug("Token-refresh feilet med {Code} — sender responsen videre", refreshResult.Error?.Code);
            return response;
        }
        
        logger.LogDebug("Token refreshet vellyket - prøver igjen");
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
        
        string? userId = httpContext.User.FindFirst("sub")?.Value;
        if (userId == null)
            return Result.Failure(AppError.Create(ErrorCode.Unauthorized, "Ingen bruker-ID i claims."));
        
        Result<RefreshRecord> result = await tokenRefreshService.RefreshPairAsync(userId, httpContext, ct);
        if (result.IsFailure)
            return Result.Failure(result.Error!);

        tokenRefreshService.ApplyTokenPair(httpContext, result.Value!);
        return Result.Success();
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