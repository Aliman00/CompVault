using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity.Data;

namespace CompVault.Frontend.Features.Auth.Services;

public class AuthService(ILogger<AuthService> logger) : IAuthService
{
    /// <inheritdoc />
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Login attempt from Email: {@Payload}", new { email = request.Email});


            var response =
                await _httpClient.PostAsJsonAsync(ApiEndpoints.AuthLogin, request, cancellationToken);
      
            var result = await response.ToResultAsync<LoginResponse>(cancellationToken);


            if (!result.IsSuccess || result.Data?.Token == null)
            {
                logger.LogWarning("Login failed: {Error}", result.ErrorMessage);
                return result;
            }


            await tokenService.SetTokenAsync(result.Data.Token);


            customAuthStateProvider.NotifyUserAuthentication(result.Data.Token);
      
            return result;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error during login attempt");
            return Result<LoginResponse>.Failure("Connection failed. Please check your internet.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occured");
            return Result<LoginResponse>.Failure("Unexpected error occured. Try again later.");
        }
    }

}