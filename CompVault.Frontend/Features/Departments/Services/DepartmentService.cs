using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Departments.Services;

public class DepartmentService(
    ILogger<DepartmentService> logger,
    IHttpClientFactory httpClientFactory) : IDepartmentService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<DepartmentDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Department.Base, ct);

            Result<List<DepartmentDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DepartmentDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DepartmentDto>>.Failure(result.Error!);

            return Result<List<DepartmentDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av avdelinger");
            return Result<List<DepartmentDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av avdelinger");
            return Result<List<DepartmentDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto?>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Department.ById(id), ct);

            Result<DepartmentDto?> result =
                await HttpClientExtensions.ParseResponseAsync<DepartmentDto?>(response, ct);

            if (result.IsFailure)
                return Result<DepartmentDto?>.Failure(result.Error!);

            return Result<DepartmentDto?>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av avdeling");
            return Result<DepartmentDto?>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av avdeling");
            return Result<DepartmentDto?>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.Department.ById(id), request, ct);

            Result<DepartmentDto> result = await HttpClientExtensions.ParseResponseAsync<DepartmentDto>(response, ct);

            if (result.IsFailure)
                return Result<DepartmentDto>.Failure(result.Error!);

            return Result<DepartmentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av avdeling {Id}", id);
            return Result<DepartmentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av avdeling {Id}", id);
            return Result<DepartmentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.Department.Base, request, ct);

            Result<DepartmentDto> result = await HttpClientExtensions.ParseResponseAsync<DepartmentDto>(response, ct);

            if (result.IsFailure)
                return Result<DepartmentDto>.Failure(result.Error!);

            return Result<DepartmentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av avdeling");
            return Result<DepartmentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av avdeling");
            return Result<DepartmentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }


    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(ApiRoutes.Department.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av avdeling {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av avdeling {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}