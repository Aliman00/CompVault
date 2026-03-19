using CompVault.Frontend.Common.Configuration;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthService> _logger;

    // Midlertidig hardkodet for testing
    private const string DemoEmail = "test@compvault.no";
    private const string DemoOtp = "123456";

    private bool _isAuthenticated;

    public bool IsAuthenticated => _isAuthenticated;

    public AuthService(ILogger<AuthService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(BackendApiSettings.ClientName);
    }

    /// <inheritdoc />
    public async Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Request OTP: {@Payload}", request);

            // For testing: Aksepter kun demo-email
            if (!request.Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(AppError.Create(ErrorCode.NotFound, "Email not found"));
            }

            // Sender Http-forespørselen med requesten
            var response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, request, ct);

            // Hvis det gikk galt, returner feilmeldingen
            if (!response.IsSuccessStatusCode)
            {
                var problemDetail = await response.Content.ReadFromJsonAsync<ProblemDetail>(ct);

                if (problemDetail == null)
                    return Result.Failure(AppError.Create(ErrorCode.Unknown, "Unknown error from server"));

                if (!Enum.TryParse<ErrorCode>(problemDetail.Code, out var errorCode))
                    errorCode = ErrorCode.Unknown;

                return Result.Failure(AppError.Create(errorCode, problemDetail.Message));
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during login attempt");
            // For testing: Fortsett selv om backend ikke er tilgjengelig
            if (request.Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Success();
            }
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Connection failed. Please check your internet."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred");
            // For testing: Fortsett selv om backend ikke er tilgjengelig
            if (request.Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Success();
            }
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Unexpected error occurred. Try again later."));
        }
    }

    /// <inheritdoc />
    public Task<Result> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Verify OTP for: {Email}", request.Email);

        // Midlertidig hardkodet verifisering for testing
        if (!request.Email.Equals(DemoEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Result.Failure(AppError.Create(ErrorCode.NotFound, "Email not found")));
        }

        if (request.OtpCode != DemoOtp)
        {
            return Task.FromResult(Result.Failure(AppError.Create(ErrorCode.InvalidCredentials, "Invalid OTP code")));
        }

        _isAuthenticated = true;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public void Logout()
    {
        _isAuthenticated = false;
    }
}
