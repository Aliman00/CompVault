using System.Net;
using System.Net.Http.Headers;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Services;

/// <summary>
/// Håndterer token mellom HttpClient og backend. Oppdaterer tokenparet hvis AccessToken er utgått, men vi har
/// gyldig RefreshToken
/// </summary>
public class AuthTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthTokenHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Henter instansene som er på samme scope til den aktive kretsen
        IServiceProvider services = httpContextAccessor.HttpContext!.RequestServices;
        TokenProvider tokenProvider = services.GetRequiredService<TokenProvider>();
        AuthStateProvider authStateProvider = services.GetRequiredService<AuthStateProvider>();
        
        // Legger alltid til et access token hvis vi har et
        if (tokenProvider.AccessToken != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);

        // En HttpRequestMessage er single use, så vi kloner den for å kunne sende igjen hvis vi må fornye token
        HttpRequestMessage retryRequest = await CloneRequestAsync(request);
        HttpResponseMessage response = await base.SendAsync(request, ct);
        
        // Hvis vi har et gyldig refresh token, men får 401 så prøver vi å fornye access token i bakgrunn automatisk
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            bool tokensRefreshed = await TryRefreshTokenAsync(authStateProvider, ct);

            if (tokensRefreshed)
            {
                // Prøver det originale kallet med oppdatert token
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", 
                    tokenProvider.AccessToken);
                response = await base.SendAsync(retryRequest, ct);
            }
            else
            {
                // Refresh token er ugyldig eller utgått
                logger.LogWarning("Token-refresh feilet — logger ut brukeren");
                authStateProvider.MarkUserAsLoggedOut();
            }
        }

        return response;
    }
    
    // Vi kloner en request siden hver request er single-use og blir brukt opp
    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (KeyValuePair<string, IEnumerable<string>> header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        
        // Hvis requesten har en body, så må vi kopiere den
        if (original.Content != null)
        {
            byte[] body = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(body);
            
            foreach (KeyValuePair<string, IEnumerable<string>> header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
    
    // Hvis brukeren har gyldig refresh token, så byttes token med et nytt par - kalles bare hvis utgått token
    private async Task<bool> TryRefreshTokenAsync(
        AuthStateProvider authStateProvider,
        CancellationToken ct)
    {
        try
        {   // Bruker en egen HttpClient for å unngå å kalle backend med utgått token
            HttpClient httpClient = httpClientFactory.CreateClient(BackendApiSettings.AuthClientName);
            
            HttpResponseMessage response =
                await httpClient.PostAsync(ApiRoutes.Auth.RefreshFull, null, ct);

            Result<AccessTokenResponse> accessTokenResult =
                await HttpClientExtensions.ParseResponseAsync<AccessTokenResponse>(response, ct);
            
            if (accessTokenResult.IsFailure)
            {
                logger.LogWarning("Oppdatering av tokens returnerte feil: [{ErrorCode}] {Message}",
                    accessTokenResult.Error!.Code, accessTokenResult.Error.Message);
                return false;
            }
            
            // TODO: Fjern logging etterhvert, har den for testing enn så lenge
            logger.LogInformation("Token oppdatert vellykket!");
            authStateProvider.UpdateAccessToken(accessTokenResult.Value!.AccessToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av tokens");
            return false;
        }
    }
}