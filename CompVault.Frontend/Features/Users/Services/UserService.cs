using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Users.Services;

public class UserService(
    ILogger<UserService> logger,
    IHttpClientFactory httpClientFactory) : IUserService
{

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<UserDto>>> GetAllUsersAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.User.Base, ct);

            Result<List<UserDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<UserDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<UserDto>>.Failure(result.Error!);

            return Result<List<UserDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av brukere");
            return Result<List<UserDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av brukere");
            return Result<List<UserDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}