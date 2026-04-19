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
    public async Task<Result<List<UserDto>>> GetAllAsync(CancellationToken ct)
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

    /// <inheritdoc />
    public async Task<Result<UserDto?>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.User.ById(id), ct);

            Result<UserDto?> result =
                await HttpClientExtensions.ParseResponseAsync<UserDto?>(response, ct);

            if (result.IsFailure)
                return Result<UserDto?>.Failure(result.Error!);

            return Result<UserDto?>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av brukere");
            return Result<UserDto?>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av brukere");
            return Result<UserDto?>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.User.ById(id), request, ct);

            Result<UserDto> result = await HttpClientExtensions.ParseResponseAsync<UserDto>(response, ct);

            if (result.IsFailure)
                return Result<UserDto>.Failure(result.Error!);

            return Result<UserDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av bruker {Id}", id);
            return Result<UserDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av bruker {Id}", id);
            return Result<UserDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.User.Base, request, ct);

            Result<UserDto> result = await HttpClientExtensions.ParseResponseAsync<UserDto>(response, ct);

            if (result.IsFailure)
                return Result<UserDto>.Failure(result.Error!);

            return Result<UserDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av bruker");
            return Result<UserDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av bruker");
            return Result<UserDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }


    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(ApiRoutes.User.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av bruker {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av bruker {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}