using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Equipment.Services;

public class EquipmentIssuancesService(
    ILogger<EquipmentIssuancesService> logger,
    IHttpClientFactory httpClientFactory) : IEquipmentIssuancesService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<PagedResult<EquipmentIssuanceDto>>> GetAllAsync(PagedQuery query, CancellationToken ct)
    {
        try
        {
            string url = $"{ApiRoutes.EquipmentIssuances.Base}?page={query.Page}&pageSize={query.PageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(url, ct);

            Result<PagedResult<EquipmentIssuanceDto>> result =
                await HttpClientExtensions.ParseResponseAsync<PagedResult<EquipmentIssuanceDto>>(response, ct);

            if (result.IsFailure)
                return Result<PagedResult<EquipmentIssuanceDto>>.Failure(result.Error!);

            return Result<PagedResult<EquipmentIssuanceDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av alle utleveringer");
            return Result<PagedResult<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av alle utleveringer");
            return Result<PagedResult<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentIssuanceDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.EquipmentIssuances.ById(id), ct);

            Result<EquipmentIssuanceDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentIssuanceDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentIssuanceDto>.Failure(result.Error!);

            return Result<EquipmentIssuanceDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utlevering {Id}", id);
            return Result<EquipmentIssuanceDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utlevering {Id}", id);
            return Result<EquipmentIssuanceDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EquipmentIssuanceDto>>> GetByUserAsync(Guid userId, PagedQuery query, 
        CancellationToken ct)
    {
        try
        {
            string url = $"{ApiRoutes.EquipmentIssuances.ByUser(userId)}?page={query.Page}&pageSize={query.PageSize}";
            HttpResponseMessage response = await _httpClient.GetAsync(url, ct);

            Result<PagedResult<EquipmentIssuanceDto>> result =
                await HttpClientExtensions.ParseResponseAsync<PagedResult<EquipmentIssuanceDto>>(response, ct);

            if (result.IsFailure)
                return Result<PagedResult<EquipmentIssuanceDto>>.Failure(result.Error!);

            return Result<PagedResult<EquipmentIssuanceDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utleveringer for bruker {UserId}", userId);
            return Result<PagedResult<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utleveringer for bruker {UserId}", userId);
            return Result<PagedResult<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<List<UserEquipmentCategoryDto>>> GetMyCategoriesAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.EquipmentIssuances.MyCategories, ct);

            Result<List<UserEquipmentCategoryDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<UserEquipmentCategoryDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<UserEquipmentCategoryDto>>.Failure(result.Error!);

            return Result<List<UserEquipmentCategoryDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utstyrskategorier for bruker");
            return Result<List<UserEquipmentCategoryDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utstyrskategorier for bruker");
            return Result<List<UserEquipmentCategoryDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<PagedResult<EquipmentIssuanceDto>>> GetMyEquipmentAsync(
        Guid? categoryId, PagedQuery query, CancellationToken ct)
    {
        try
        {
            string url = $"{ApiRoutes.EquipmentIssuances.My}?page={query.Page}&pageSize={query.PageSize}";
            if (categoryId.HasValue)
                url += $"&categoryId={categoryId.Value}";

            HttpResponseMessage response = await _httpClient.GetAsync(url, ct);

            Result<PagedResult<EquipmentIssuanceDto>> result =
                await HttpClientExtensions.ParseResponseAsync<PagedResult<EquipmentIssuanceDto>>(response, ct);

            if (result.IsFailure)
                return Result<PagedResult<EquipmentIssuanceDto>>.Failure(result.Error!);

            return Result<PagedResult<EquipmentIssuanceDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utstyr for bruker");
            return Result<PagedResult<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utstyr for bruker");
            return Result<PagedResult<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<List<EquipmentIssuanceDto>>> GetByItemAsync(Guid itemId, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.EquipmentIssuances.ByItem(itemId), ct);
            
            Result<List<EquipmentIssuanceDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<EquipmentIssuanceDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<EquipmentIssuanceDto>>.Failure(result.Error!);

            return Result<List<EquipmentIssuanceDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av utleveringer for utstyr {ItemId}", itemId);
            return Result<List<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av utleveringer for utstyr {ItemId}", itemId);
            return Result<List<EquipmentIssuanceDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentIssuanceDto>> CreateAsync(CreateEquipmentIssuanceRequest request, 
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.EquipmentIssuances.Base, request, ct);

            Result<EquipmentIssuanceDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentIssuanceDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentIssuanceDto>.Failure(result.Error!);

            return Result<EquipmentIssuanceDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av utlevering");
            return Result<EquipmentIssuanceDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av utlevering");
            return Result<EquipmentIssuanceDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentIssuanceDto>> UpdateAsync(Guid id, UpdateEquipmentIssuanceRequest request, 
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.EquipmentIssuances.ById(id), request, ct);

            Result<EquipmentIssuanceDto> result =
                await HttpClientExtensions.ParseResponseAsync<EquipmentIssuanceDto>(response, ct);

            if (result.IsFailure)
                return Result<EquipmentIssuanceDto>.Failure(result.Error!);

            return Result<EquipmentIssuanceDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av utlevering {Id}", id);
            return Result<EquipmentIssuanceDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av utlevering {Id}", id);
            return Result<EquipmentIssuanceDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.DeleteAsync(ApiRoutes.EquipmentIssuances.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av utlevering {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av utlevering {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}