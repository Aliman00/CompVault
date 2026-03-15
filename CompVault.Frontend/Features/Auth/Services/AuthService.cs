using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity.Data;

namespace CompVault.Frontend.Features.Auth.Services;

public class AuthService(ILogger<AuthService> logger, IHttpClientFactory httpClientFactory) : IAuthService
{
    /// <summary>
    /// HttpClient mot backend
    /// </summary>
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("BackendApi");
    
    /// <inheritdoc />
    public async Task<Result> RequestOtpAsync(LoginRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Request OTP: {@Payload}", request);


            var response = await _httpClient.PostAsJsonAsync(, request, ct);
      
            var result = await response.


            if (!result.IsSuccess || result.Data?.Token == null)
            {
                logger.LogWarning("Login failed: {Error}", result.ErrorMessage);
                return result;
            }
      
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error during login attempt");
            return Result.Failure("Connection failed. Please check your internet.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occured");
            return Result.Failure("Unexpected error occured. Try again later.");
        }
    }

}