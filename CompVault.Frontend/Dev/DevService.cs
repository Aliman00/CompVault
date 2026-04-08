using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Dev;

public class DevService(
    ILogger<DevService> logger,
    IHttpClientFactory httpClientFactory) : IDevService
{
    /// <summary>
    /// HttpClient mot backend
    /// </summary>
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result> RequestOtpAsync(RequestOtpRequest request)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync("api/auth/dev-create-otp", request);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, CancellationToken.None);
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