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
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

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

            Result<AccessTokenResponse> result =
                await HttpClientExtensions.ParseResponseAsync<AccessTokenResponse>(response, ct);

            if (result.IsFailure)
                return Result.Failure(result.Error!);
            
            authStateProvider.MarkUserAsAuthenticated(result.Value!.AccessToken);
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
    
    /// <inheritdoc />
    public async Task<Result> RefreshTokenAsync(CancellationToken ct)
    {
        try
        {
            // Bruker AuthClient for å unngå kall med AuthTokenHandler. Det kan føre til en loop
            HttpClient httpClient = httpClientFactory.CreateClient(BackendApiSettings.AuthClientName);
            HttpResponseMessage response = await httpClient.PostAsync(ApiRoutes.Auth.RefreshFull, 
                null, ct);

            Result<AccessTokenResponse> result =
                await HttpClientExtensions.ParseResponseAsync<AccessTokenResponse>(response, ct);

            if (result.IsFailure)
                return Result.Failure(result.Error!);
            
            authStateProvider.MarkUserAsAuthenticated(result.Value!.AccessToken);
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved token-refresh ved oppstart");
            return Result.Failure(AppError.Create(ErrorCode.NetworkError, "Tilkoblingen feilet."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved token-refresh ved oppstart");
            return Result.Failure(AppError.Create(ErrorCode.Unknown, "Noe gikk galt."));
        }
    }
    
    /// <inheritdoc />
    public async Task LogOutAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(ApiRoutes.Auth.RevokeFull, null, ct);
            Result revokeResult = await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
            if (revokeResult.IsFailure)
                logger.LogWarning("Token-revokering feilet: [{ErrorCode}] {Message}",
                    revokeResult.Error!.Code, revokeResult.Error.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet  feil ved utlogging");
        }
        finally
        {
            authStateProvider.MarkUserAsLoggedOut();
        }
    }
}