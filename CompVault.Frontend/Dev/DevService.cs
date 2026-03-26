using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Dev;

public class DevService(
    ILogger<DevService> logger, 
    IHttpClientFactory httpClientFactory,
    AuthStateProvider authStateProvider) : IDevService
{
    /// <summary>
    /// HttpClient mot backend
    /// </summary>
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);
    
    /// <inheritdoc />
    public async Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync("api/dev/verifyotp", request, ct);
            
            Result<RefreshTokenResponse> result =
                await HttpClientExtensions.ParseResponseAsync<RefreshTokenResponse>(response, ct);

            if (result.IsFailure)
                return Result.Failure(result.Error!);
            
            authStateProvider.MarkUserAsAuthenticated(result.Value!.AccessToken);
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved dev-innlogging for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError, "Tilkoblingen feilet."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved dev-innlogging for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.Unknown, "Noe gikk galt."));
        }
    }
}