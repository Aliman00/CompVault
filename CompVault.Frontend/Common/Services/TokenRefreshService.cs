using System.Collections.Concurrent;
using System.Security.Claims;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Http.Models;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Services;

public class TokenRefreshService(
    IHttpClientFactory httpClientFactory,
    AuthSettings authSettings, 
    IWebHostEnvironment env, 
    ILogger<TokenRefreshService> logger) : ITokenRefreshService
{
    // En ordbok som sjekker om et kall holder på å refreshe token, slik at parallelle kall venter på samme refresh
    private static readonly ConcurrentDictionary<string, AsyncLazy<Result<RefreshRecord>>> PendingRefreshes = new();
    
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastRefreshed = new();

    /// <summary>
    /// Refresher token par for innlogget bruker. Paralelle kall venter på samme refresh-operasjon,
    /// slik at både CookieValidationEvents og AccessTokenHandler ikke kjører om hverandre
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="httpContext"></param>
    /// <param name="ct"></param>
    /// <returns>Result med RefreshRecord som inneholder token-par og tiden de ble satt</returns>
    public async Task<Result<RefreshRecord>> RefreshPairAsync(string userId, HttpContext httpContext,
        CancellationToken ct = default)
    {
        if (LastRefreshed.TryGetValue(userId, out DateTimeOffset lastRefreshed) &&
            lastRefreshed > DateTimeOffset.UtcNow.AddMinutes(-authSettings.ValidationIntervalMinutes))
        {
            logger.LogDebug("Ingen vits å oppdatere refresh - nylig oppdatert");
            return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown, "Nylig oppdatert"));
        }
            
        
        // Sjekker om det finnes en eksisterende nøkkel med med denne bruker ID-en.
        // Dette sikrer at parallelle requester bruker samme RefreshAsync-instance.
        // Lazy sikrer at den ikke kjører før vi kaller den selv
        AsyncLazy<Result<RefreshRecord>> pendingRefresh = PendingRefreshes.GetOrAdd(userId,
            _ => new AsyncLazy<Result<RefreshRecord>>(() => GetTokenPairAsync(httpContext, ct)));
        
        Result<RefreshRecord> result = await pendingRefresh;
        
        if (result.IsSuccess)
            LastRefreshed[userId] = result.Value!.RefreshedAt;
        
        // Hver forespørsel rydder opp etter seg selv
        // Fjerner med forsinkelse så samtidige requests rekker å treffe samme lazy
        _ = RemoveAfterDelayAsync(userId);
        
        return result;
    }
    
    private static async Task RemoveAfterDelayAsync(string userId)
    {
        await Task.Delay(500);
        PendingRefreshes.TryRemove(userId, out _);
    }
    
    // Utfører API-kall mot backend og henter token-par
    private async Task<Result<RefreshRecord>> GetTokenPairAsync(HttpContext httpContext, CancellationToken ct = default)
    {
        string? refreshToken = httpContext.GetRefreshTokenCookie();
        if (string.IsNullOrEmpty(refreshToken))
        {
            
            logger.LogWarning("Ingen refresh token funnet - logger brukeren ut");
            return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.NotFound, 
                "Ingen refresh token funnet"));
        }
        
        // Oppretter en klient med AuthClienten uten påkoblet AccessTokenHandler
        HttpClient client = httpClientFactory.CreateClient(BackendApiSettings.AuthClientName);

        try
        {
            var refreshTokenRequest = new RefreshTokenRequest { RefreshToken = refreshToken };
            HttpResponseMessage refreshTokenResponse = await client.PostAsJsonAsync(
                ApiRoutes.Auth.RefreshFull, refreshTokenRequest, ct);
            
            // Sjekker om errorkoden er AccountInactive, det setter brukeren som utlogget.
            // Hvis feks race condition - to requester kjører nesten paralellelt så sender backend InvalidToken.
            // Vi lar den faile åpent, i og med at en av forespørslene kan ha refreshet token
            if (!refreshTokenResponse.IsSuccessStatusCode)
            {
                ProblemDetail? problem = await refreshTokenResponse.Content
                    .ReadFromJsonAsync<ProblemDetail>(ct);

                if (problem?.Code == nameof(ErrorCode.AccountInactive))
                {
                    // Bruker er deaktivert — logget ut både fra context og sletter token
                    logger.LogWarning("Bruker er deaktivert — logger brukeren ut");
                    return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unauthorized, 
                        "Bruker deaktivitert"));
                }
                
                // Backend kan returnere annen feil f.eks. ved server nede. Forblir innlogget, og AccessTokenHandler
                // håndterer 401 på neste kall til backend
                logger.LogDebug("Token-refresh feilet med {Code}", problem?.Code);
                return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown, 
                    "Token refresh failet"));
            }
            
            TokenResponse? tokenResponse = await refreshTokenResponse.Content
                .ReadFromJsonAsync<TokenResponse>(ct);

            if (tokenResponse == null)
            {
                logger.LogWarning("Tom respons fra backend ved token-refresh — feiler åpent");
                return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown, 
                    "Tom response fra backend ved token-refresh"));
            }
            
            var refreshRecord =
                new RefreshRecord(tokenResponse.AccessToken, tokenResponse.RefreshToken, DateTimeOffset.UtcNow);
            
            logger.LogDebug("Token refreshet vellykket");
            return Result<RefreshRecord>.Success(refreshRecord);
        }
        catch (Exception ex)
        {
            // Feiler åpent hvis backend er nede
            logger.LogError(ex, "Uventet feil ved token-refresh i TokenRefreshService");
            return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Uventet feil ved token-refresh"));
        }
    }
    
    /// <summary>
    /// Oppdaterer en HttpContext med acess-token i claim og legger til refresh token-cookie
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="refreshRecord"></param>
    public void ApplyTokenPair(HttpContext httpContext, RefreshRecord refreshRecord)
    {
        // Principal og Identity er garantert satt av cookie-middlewaren
        if (httpContext.User.Identity is not ClaimsIdentity identity)
            return;
            
        // Bytter ut gammel claim med ny
        Claim? oldClaim = identity.FindFirst("access_token");
        if (oldClaim != null) 
            identity.RemoveClaim(oldClaim);
        identity.AddClaim(new Claim("access_token", refreshRecord.AccessToken));
            
        httpContext.AppendRefreshTokenCookie(refreshRecord.RefreshToken, authSettings, env);
    }
}