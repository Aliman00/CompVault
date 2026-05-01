using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Equipment.Services;

public class EquipmentCategoryService(
    ILogger<EquipmentCategoryService> logger,
    IHttpClientFactory httpClientFactory) : IEquipmentCategoryService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<EquipmentCategoryDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.EquipmentCategories.Base, ct);

            Result<List<EquipmentCategoryDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<EquipmentCategoryDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<EquipmentCategoryDto>>.Failure(result.Error!);

            return Result<List<EquipmentCategoryDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av alle utstyrskategorier");
            return Result<List<EquipmentCategoryDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av alle utstyrskategorier");
            return Result<List<EquipmentCategoryDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentCategoryDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.EquipmentCategories.ById(id), ct);

            Result<EquipmentCategoryDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentCategoryDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentCategoryDto>.Failure(result.Error!);

            return Result<EquipmentCategoryDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utstyrskategori {Id}", id);
            return Result<EquipmentCategoryDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utstyrskategori {Id}", id);
            return Result<EquipmentCategoryDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentCategoryDto>> CreateAsync(CreateEquipmentCategoryRequest request,
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.EquipmentCategories.Base, request, ct);

            Result<EquipmentCategoryDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentCategoryDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentCategoryDto>.Failure(result.Error!);

            return Result<EquipmentCategoryDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av utstyrskategori");
            return Result<EquipmentCategoryDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av utstyrskategori");
            return Result<EquipmentCategoryDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentCategoryDto>> UpdateAsync(Guid id, UpdateEquipmentCategoryRequest request,
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.EquipmentCategories.ById(id), request, ct);

            Result<EquipmentCategoryDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentCategoryDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentCategoryDto>.Failure(result.Error!);

            return Result<EquipmentCategoryDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av utstyrskategori {Id}", id);
            return Result<EquipmentCategoryDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av utstyrskategori {Id}", id);
            return Result<EquipmentCategoryDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.DeleteAsync(ApiRoutes.EquipmentCategories.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av utstyrskategori {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av utstyrskategori {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}