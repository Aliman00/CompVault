using System.Security.Claims;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Auth.Services;

public class AuthService(
    ILogger<AuthService> logger,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor) : IAuthService
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
    public async Task<Result<TokenResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request, ct);

            return await HttpClientExtensions.ParseResponseAsync<TokenResponse>(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved OTP-verifisering for {Email}", request.Email);
            return Result<TokenResponse>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved OTP-verifisering for {Email}", request.Email);
            return Result<TokenResponse>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }


    /// <inheritdoc />
    public async Task LogOutAsync(CancellationToken ct)
    {
        try
        {
            string? refreshToken = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

            // Hvis vi ikke har en refresh token-cookie, så skipper vi å revoke. Backend sin DataAnnotations 
            // fanger opp requester uten refresh token
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                logger.LogInformation("Ingen refreshToken-cookie funnet ved utlogging; hopper over token-revokering.");
                return;
            }

            // Sender refresh token i body
            var revokeRequest = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Auth.RevokeFull)
            {
                Content = JsonContent.Create(new RefreshTokenRequest { RefreshToken = refreshToken })
            };

            HttpResponseMessage response = await _httpClient.SendAsync(revokeRequest, ct);
            Result revokeResult = await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);

            if (revokeResult.IsFailure)
                logger.LogWarning("Token-revokering feilet: [{ErrorCode}] {Message}",
                    revokeResult.Error!.Code, revokeResult.Error.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet  feil ved utlogging");
        }
    }

}