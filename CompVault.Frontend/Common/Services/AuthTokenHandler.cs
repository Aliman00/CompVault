using System.Net;
using System.Net.Http.Headers;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Services;

public class AuthTokenHandler(
    TokenProvider tokenProvider,
    AuthStateProvider authStateProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthTokenHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Legger alltid til et access token hvis vi har et
        if (tokenProvider.AccessToken != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);

        HttpResponseMessage response = await base.SendAsync(request, ct);
        
        // Hvis vi har et gydlig refresh token, men får 401 så prøver vi å fornye access token i bakgrunn automatisk
        if (response.StatusCode == HttpStatusCode.Unauthorized && tokenProvider.RefreshToken != null)
        {
            bool tokensRefreshed = await TryRefreshTokenAsync(ct);

            if (tokensRefreshed)
            {
                // Prøver det originale kallet med nye tokens
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", 
                    tokenProvider.AccessToken);
                response = await base.SendAsync(request, ct);
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
    
    // Hvis brukeren har gyldig refresh token, så byttes token med et nytt par - kalles bare hvis utgått token
    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        try
        {   // Bruker en egen HttpClient for å unngå å kalle backend med utgått token
            HttpClient httpClient = httpClientFactory.CreateClient(BackendApiSettings.ClientName);

            var refreshRequest = new RefreshTokenRequest { RefreshToken = tokenProvider.RefreshToken! };

            HttpResponseMessage response =
                await httpClient.PostAsJsonAsync(ApiRoutes.Auth.RefreshFull, refreshRequest, ct);

            Result<RefreshTokenResponse> refreshTokenResult =
                await HttpClientExtensions.ParseResponseAsync<RefreshTokenResponse>(response, ct);
            if (refreshTokenResult.IsFailure)
            {
                logger.LogWarning("Oppdatering av tokens returnerte feil: [{ErrorCode}] {Message}",
                    refreshTokenResult.Error!.Code, refreshTokenResult.Error.Message);
                return false;
            }
            
            // TODO: Fjern logging etterhvert, har den for testing enn så lenge
            logger.LogInformation("Token oppdatert vellyket!");
            authStateProvider.UpdateAccessToken(refreshTokenResult.Value!.AccessToken,
                refreshTokenResult.Value!.RefreshToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av tokens");
            return false;
        }
    }
}