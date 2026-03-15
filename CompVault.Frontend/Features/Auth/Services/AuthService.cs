using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Auth.Services;

public class AuthService(ILogger<AuthService> logger, IHttpClientFactory httpClientFactory) : IAuthService
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
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.RequestOtp, request, ct);
            
            // Hvis det gikk galt, returner feilmeldingen
            if (!response.IsSuccessStatusCode)
            {
                var problemDetail = await response.Content.ReadFromJsonAsync<ProblemDetail>(ct);

                if (problemDetail == null)
                    return Result.Failure(AppError.Create(ErrorCode.Unknown, "Unknown error from server"));
                
                if (!Enum.TryParse<ErrorCode>(problemDetail.Code, out var errorCode))
                    errorCode = ErrorCode.Unknown; // Fallback til Unknown hvis ingen kode med
                
                return Result.Failure(AppError.Create(errorCode, problemDetail.Message));
            }
            
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error during login attempt");
            return Result.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Connection failed. Please check your internet."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occured");
            return Result.Failure(AppError.Create(ErrorCode.Unknown, 
                "Unexpected error occured. Try again later."));
        }
    }

}