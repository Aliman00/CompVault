using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public class DocumentTypeCategoryService(
    ILogger<DocumentTypeCategoryService> logger,
    IHttpClientFactory httpClientFactory) : IDocumentTypeCategoryService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);
    
    /// <inheritdoc />
    public async Task<Result<List<DocumentTypeCategoryDto>>> GetAllAsync(string documentTypeSlug, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.GetAsync(ApiRoutes.DocumentTypeCategories.All(documentTypeSlug), ct);

            Result<List<DocumentTypeCategoryDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DocumentTypeCategoryDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DocumentTypeCategoryDto>>.Failure(result.Error!);

            return Result<List<DocumentTypeCategoryDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av kategorier for {Slug}", documentTypeSlug);
            return Result<List<DocumentTypeCategoryDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av kategorier for {Slug}", documentTypeSlug);
            return Result<List<DocumentTypeCategoryDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<DocumentTypeCategoryDto>> CreateAsync(string documentTypeSlug,
        CreateDocumentTypeCategoryRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PostAsJsonAsync(
                    ApiRoutes.DocumentTypeCategories.All(documentTypeSlug), request, ct);

            Result<DocumentTypeCategoryDto> result =
                await HttpClientExtensions.ParseResponseAsync<DocumentTypeCategoryDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentTypeCategoryDto>.Failure(result.Error!);

            return Result<DocumentTypeCategoryDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av kategori for {Slug}", documentTypeSlug);
            return Result<DocumentTypeCategoryDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av kategori for {Slug}", documentTypeSlug);
            return Result<DocumentTypeCategoryDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<DocumentTypeCategoryDto>> UpdateAsync(string documentTypeSlug, Guid categoryId,
        UpdateDocumentTypeCategoryRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PutAsJsonAsync(
                    ApiRoutes.DocumentTypeCategories.ById(documentTypeSlug, categoryId), request, ct);

            Result<DocumentTypeCategoryDto> result = await HttpClientExtensions.ParseResponseAsync<DocumentTypeCategoryDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentTypeCategoryDto>.Failure(result.Error!);

            return Result<DocumentTypeCategoryDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av kategori {CategoryId} for {Slug}",
                categoryId, documentTypeSlug);
            return Result<DocumentTypeCategoryDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av kategori {CategoryId} for {Slug}",
                categoryId, documentTypeSlug);
            return Result<DocumentTypeCategoryDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string documentTypeSlug, Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.DeleteAsync(ApiRoutes.DocumentTypeCategories.ById(documentTypeSlug, id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av kategori {CategoryId} for {Slug}", 
                id, documentTypeSlug);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av kategori {CategoryId} for {Slug}", 
                id, documentTypeSlug);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}