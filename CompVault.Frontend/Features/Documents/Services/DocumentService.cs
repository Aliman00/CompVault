using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Models;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public class DocumentService(
    ILogger<DocumentService> logger,
    IHttpClientFactory httpClientFactory) : IDocumentService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<DocumentListDto>>> GetAllAsync(string slug, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.Documents.Base(slug), ct);

            Result<List<DocumentListDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DocumentListDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DocumentListDto>>.Failure(result.Error!);

            return Result<List<DocumentListDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av dokumenter for {Slug}", slug);
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av dokumenter for {Slug}", slug);
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<DocumentListDto>>> GetUserDocumentsAsync(
        DocumentQueryParameters query, CancellationToken ct)
    {
        try
        {
            string url = BuildFilterUrl(ApiRoutes.Documents.MyDocuments, query);
            HttpResponseMessage response = await _httpClient.GetAsync(url, ct);

            Result<PagedResult<DocumentListDto>> result =
                await HttpClientExtensions.ParseResponseAsync<PagedResult<DocumentListDto>>(response, ct);

            if (result.IsFailure)
                return Result<PagedResult<DocumentListDto>>.Failure(result.Error!);

            return Result<PagedResult<DocumentListDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av mine dokumenter");
            return Result<PagedResult<DocumentListDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av mine dokumenter");
            return Result<PagedResult<DocumentListDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> GetByIdAsync(string slug, Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(
                ApiRoutes.Documents.ById(slug, id), ct);

            Result<DocumentDto> result =
                await HttpClientExtensions.ParseResponseAsync<DocumentDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentDto>.Failure(result.Error!);

            return Result<DocumentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av dokumente {Slug}/{DocumentId}",
                slug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av dokument {Slug}/{DocumentId}",
                slug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> CreateAsync(string slug, CreateDocumentRequest request,
        FileAttachment? file, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsMultipartFormAsync(ApiRoutes.Documents.Base(slug), 
                    request, file, ct);

            Result<DocumentDto> result = await HttpClientExtensions.ParseResponseAsync<DocumentDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentDto>.Failure(result.Error!);

            return Result<DocumentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av dokumenter");
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av dokumenter");
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> UpdateAsync(string slug, Guid id, UpdateDocumentRequest request,
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.Documents.ById(slug, id), request, ct);

            Result<DocumentDto> result = await HttpClientExtensions.ParseResponseAsync<DocumentDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentDto>.Failure(result.Error!);

            return Result<DocumentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av dokument {Slug}/{Id}", slug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av dokument {Slug}/{Id}", slug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<DocumentDto>> UpdateVersionAsync(string slug, Guid documentId, 
        FileAttachment? file, CancellationToken ct)
    {
        try
        { // Bygger FormFile med filen
            using var content = new MultipartFormDataContent();
            MultipartFormBuilder.AddFile(content, file);
            
            HttpResponseMessage response =
                await _httpClient.PostAsync(
                    ApiRoutes.Documents.UploadVersion(slug, documentId), content, ct);

            Result<DocumentDto> result = await HttpClientExtensions.ParseResponseAsync<DocumentDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentDto>.Failure(result.Error!);

            return Result<DocumentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved versjonsoppdatering av dokument {Slug}/{Id}", 
                slug, documentId);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved versjonsoppdatering av dokument {Slug}/{Id}", 
                slug, documentId);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string slug, Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.DeleteAsync(ApiRoutes.Documents.ById(slug, id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av dokument {Slug}/{Id}", slug, id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av dokument {Slug}/{Id}", slug, id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<FileAttachment>> DownloadAsync(string slug, Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient
                .GetAsync(ApiRoutes.Documents.Download(slug, id), ct);

            if (!response.IsSuccessStatusCode)
            {
                Result<FileAttachment> errorResult = await HttpClientExtensions.ParseResponseAsync<FileAttachment>(response, ct);
                return Result<FileAttachment>.Failure(errorResult.Error!);
            }

            FileAttachment file = await HttpClientExtensions.ReadFileAttachmentAsync(response, ct);
            return Result<FileAttachment>.Success(file);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved nedlasting av dokument {DocumentId}", id);
            return Result<FileAttachment>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved nedlasting av dokument {DocumentId}", id);
            return Result<FileAttachment>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    // Bygger base-urlen med query-filtering
    private static string BuildFilterUrl(string baseUrl, DocumentQueryParameters query)
    {
        var queryParams = new Dictionary<string, string?>
        {
            ["page"] = query.Page.ToString(),
            ["pageSize"] = query.PageSize.ToString(),
            ["signatureFilter"] = ((int)query.SignatureFilter).ToString()
        };

        if (query.UserId.HasValue)
            queryParams["userId"] = query.UserId.ToString();
        
        if (query.DocumentTypeSlug is not null)
            queryParams["documentTypeSlug"] = query.DocumentTypeSlug;
        
        queryParams["sortBy"] = ((int)query.SortBy).ToString();
        queryParams["sortDescending"] = query.SortDescending.ToString().ToLower();

        return baseUrl.AddQueryFilter(queryParams);
    }
}