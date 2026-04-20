using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.JobTitles;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.JobTitle.Services;

public class JobTitleService(
    ILogger<JobTitleService> logger,
    IHttpClientFactory httpClientFactory) : IJobTitleService
{

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<JobTitleDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.JobTitle.Base, ct);

            Result<List<JobTitleDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<JobTitleDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<JobTitleDto>>.Failure(result.Error!);

            return Result<List<JobTitleDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av stillinger");
            return Result<List<JobTitleDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av stillinger");
            return Result<List<JobTitleDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<JobTitleDto?>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.JobTitle.ById(id), ct);

            Result<JobTitleDto?> result =
                await HttpClientExtensions.ParseResponseAsync<JobTitleDto?>(response, ct);

            if (result.IsFailure)
                return Result<JobTitleDto?>.Failure(result.Error!);

            return Result<JobTitleDto?>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av stillinger");
            return Result<JobTitleDto?>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av stillinger");
            return Result<JobTitleDto?>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<JobTitleDto>> CreateAsync(CreateJobTitleRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.JobTitle.Base, request, ct);

            Result<JobTitleDto> result = await HttpClientExtensions.ParseResponseAsync<JobTitleDto>(response, ct);

            if (result.IsFailure)
                return Result<JobTitleDto>.Failure(result.Error!);

            return Result<JobTitleDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av stilling");
            return Result<JobTitleDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av stilling");
            return Result<JobTitleDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }


    /// <inheritdoc />
    public async Task<Result<JobTitleDto>> UpdateAsync(Guid id, UpdateJobTitleRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.JobTitle.ById(id), request, ct);

            Result<JobTitleDto> result = await HttpClientExtensions.ParseResponseAsync<JobTitleDto>(response, ct);

            if (result.IsFailure)
                return Result<JobTitleDto>.Failure(result.Error!);

            return Result<JobTitleDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av stilling {Id}", id);
            return Result<JobTitleDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av stilling {Id}", id);
            return Result<JobTitleDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(ApiRoutes.JobTitle.ById(id), ct);

            if (!response.IsSuccessStatusCode)
            {
                Result<JobTitleDto> errorResult = await HttpClientExtensions.ParseResponseAsync<JobTitleDto>(response, ct);
                return Result.Failure(errorResult.Error!);
            }

            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av stilling {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av stilling {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}