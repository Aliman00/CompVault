using System.Collections.Concurrent;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Http.Models;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Services;

public class TokenRefreshService(
    IHttpClientFactory httpClientFactory,
    AuthSettings authSettings, 
    ILogger<TokenRefreshService> logger) : ITokenRefreshService
{
    // En ordbok som sjekker om et kall holder på å refreshe token, slik at parallelle kall venter på samme refresh
    private readonly ConcurrentDictionary<string, AsyncLazy<Result<RefreshRecord>>> _pendingRefreshes = new();
    
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastRefreshed = new();

    
    /// <inheritdoc />
    public async Task<Result<RefreshRecord>> RefreshPairAsync(string userId, string refreshToken,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
            return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.NotFound,
                "Ingen refresh token"));
        
        if (_lastRefreshed.TryGetValue(userId, out DateTimeOffset lastRefreshed) &&
            lastRefreshed > DateTimeOffset.UtcNow.AddMinutes(-authSettings.ValidationIntervalMinutes))
        {
            logger.LogDebug("Ingen vits å oppdatere refresh - nylig oppdatert");
            return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.RecentlyRefreshed, 
                "Nylig oppdatert"));
        }
        
        // Sjekker om det finnes en eksisterende nøkkel med med denne bruker ID-en.
        // Dette sikrer at parallelle requester bruker samme RefreshAsync-instance.
        // Lazy sikrer at den ikke kjører før vi kaller den selv. Ingen CT slik at hvis første kaller
        // avbryter, så avbrytes den ikke for de andre
        AsyncLazy<Result<RefreshRecord>> pendingRefresh = _pendingRefreshes.GetOrAdd(userId,
            _ => new AsyncLazy<Result<RefreshRecord>>(() => GetTokenPairAsync(refreshToken, 
                CancellationToken.None)));
        
        Result<RefreshRecord> result = await pendingRefresh;
        
        if (result.IsSuccess)
            _lastRefreshed[userId] = result.Value!.RefreshedAt;
        
        // Hver forespørsel rydder opp etter seg selv
        // Fjerner med forsinkelse så samtidige requests rekker å treffe samme lazy
        _ = RemoveAfterDelayAsync(userId);
        
        return result;
    }
    
    // Kjøres i bakgrunn etter vellykket refresh. Fjerner først pendingRefreshes etter en kort delay
    // så samtidige requester rekker å treffe samme AsyncLazy, og ikke prøver å refreshe igjen.
    // Deretter fjernes cooldown-oppføringen etter at ValidationIntervalMinutes har gått —
    // på det tidspunktet er den uansett utdatert
    private async Task RemoveAfterDelayAsync(string userId)
    {
        await Task.Delay(500);
        _pendingRefreshes.TryRemove(userId, out _);
        
        await Task.Delay(TimeSpan.FromMinutes(authSettings.ValidationIntervalMinutes));
        _lastRefreshed.TryRemove(userId, out _);
    }
    
    // Utfører API-kall mot backend og henter token-par
    private async Task<Result<RefreshRecord>> GetTokenPairAsync(string refreshToken, CancellationToken ct = default)
    {
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
        catch (OperationCanceledException)
        {
            // Forventet — request ble avbrutt av kalleren
            return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Token-refresh ble avbrutt"));
        }
        catch (Exception ex)
        {
            // Feiler åpent hvis backend er nede
            logger.LogError(ex, "Uventet feil ved token-refresh i TokenRefreshService");
            return Result<RefreshRecord>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Uventet feil ved token-refresh"));
        }
    }
}