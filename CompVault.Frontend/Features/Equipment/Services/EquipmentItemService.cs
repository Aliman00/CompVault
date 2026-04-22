using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Equipment.Services;

public class EquipmentItemService(
    ILogger<EquipmentItemService> logger,
    IHttpClientFactory httpClientFactory) : IEquipmentItemService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<EquipmentItemDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.EquipmentItems.Base, ct);

            Result<List<EquipmentItemDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<EquipmentItemDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<EquipmentItemDto>>.Failure(result.Error!);

            return Result<List<EquipmentItemDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av alt utstyr");
            return Result<List<EquipmentItemDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av alt utstyr");
            return Result<List<EquipmentItemDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentItemDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.EquipmentItems.ById(id), ct);

            Result<EquipmentItemDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentItemDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentItemDto>.Failure(result.Error!);

            return Result<EquipmentItemDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utstyr {Id}", id);
            return Result<EquipmentItemDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utstyr {Id}", id);
            return Result<EquipmentItemDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<EquipmentItemDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.EquipmentItems.ByCategory(categoryId), ct);
            
            Result<List<EquipmentItemDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<EquipmentItemDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<EquipmentItemDto>>.Failure(result.Error!);

            return Result<List<EquipmentItemDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utstyr for kategori {CategoryId}", categoryId);
            return Result<List<EquipmentItemDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utstyr for kategori {CategoryId}", categoryId);
            return Result<List<EquipmentItemDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentItemDto>> CreateAsync(CreateEquipmentItemRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.EquipmentItems.Base, request, ct);

            Result<EquipmentItemDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentItemDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentItemDto>.Failure(result.Error!);

            return Result<EquipmentItemDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av utstyr");
            return Result<EquipmentItemDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av utstyr");
            return Result<EquipmentItemDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentItemDto>> UpdateAsync(Guid id, UpdateEquipmentItemRequest request,
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.EquipmentItems.ById(id), request, ct);

            Result<EquipmentItemDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentItemDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentItemDto>.Failure(result.Error!);

            return Result<EquipmentItemDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av utstyr {Id}", id);
            return Result<EquipmentItemDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av utstyr {Id}", id);
            return Result<EquipmentItemDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.DeleteAsync(ApiRoutes.EquipmentItems.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av utstyr {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av utstyr {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}