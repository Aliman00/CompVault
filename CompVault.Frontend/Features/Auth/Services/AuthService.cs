using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Common.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Auth.Services;

public class AuthService(
    ILogger<AuthService> logger, 
    IHttpClientFactory httpClientFactory,
    AuthStateProvider authStateProvider) : IAuthService
{
    /// <summary>
    /// HttpClient mot backend
    /// </summary>
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.ClientName);

    /// <inheritdoc />
    public async Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Request OTP: {@Payload}", request);

            // Sender Http-forespørselen med requesten
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, 
                request, ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved OTP-forespørsel for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved OTP-forespørsel for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.Unknown, "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request, ct);

            Result<RefreshTokenResponse> result =
                await HttpClientExtensions.ParseResponseAsync<RefreshTokenResponse>(response, ct);

            if (result.IsFailure)
                return Result.Failure(result.Error!);
            
            authStateProvider.MarkUserAsAuthenticated(result.Value!.AccessToken, result.Value!.RefreshToken);
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved OTP-verifisering for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved OTP-verifisering for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.Unknown, "Noe gikk galt. Prøv igjen."));
        }
    }
    
   
}