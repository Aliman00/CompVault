using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Models;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public class DocumentService(
    ILogger<DocumentService> logger,
    IHttpClientFactory httpClientFactory) : IDocumentService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<DocumentListDto>>> GetAllAsync(string documentTypeSlug, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.Documents.Base(documentTypeSlug), ct);

            Result<List<DocumentListDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DocumentListDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DocumentListDto>>.Failure(result.Error!);

            return Result<List<DocumentListDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av dokumenter for {Slug}", documentTypeSlug);
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av dokumenter for {Slug}", documentTypeSlug);
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> GetByIdAsync(string documentTypeSlug, Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(
                ApiRoutes.Documents.ById(documentTypeSlug, id), ct);

            Result<DocumentDto> result =
                await HttpClientExtensions.ParseResponseAsync<DocumentDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentDto>.Failure(result.Error!);

            return Result<DocumentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av dokumente {Slug}/{DocumentId}",
                documentTypeSlug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av dokument {Slug}/{DocumentId}",
                documentTypeSlug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentDto>> CreateAsync(string documentTypeSlug, CreateDocumentRequest request,
        FileAttachment? file, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsMultipartFormAsync(ApiRoutes.Documents.Base(documentTypeSlug), 
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
    public async Task<Result<DocumentDto>> UpdateAsync(string documentTypeSlug, Guid id, UpdateDocumentRequest request,
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.Documents.ById(documentTypeSlug, id), request, ct);

            Result<DocumentDto> result = await HttpClientExtensions.ParseResponseAsync<DocumentDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentDto>.Failure(result.Error!);

            return Result<DocumentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av dokument {Slug}/{Id}", documentTypeSlug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av dokument {Slug}/{Id}", documentTypeSlug, id);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<DocumentDto>> UpdateVersionAsync(string documentTypeSlug, Guid documentId, 
        FileAttachment? file, CancellationToken ct)
    {
        try
        { // Bygger FormFile med filen
            using var content = new MultipartFormDataContent();
            MultipartFormBuilder.AddFile(content, file);
            
            HttpResponseMessage response =
                await _httpClient.PostAsync(
                    ApiRoutes.Documents.UploadVersion(documentTypeSlug, documentId), content, ct);

            Result<DocumentDto> result = await HttpClientExtensions.ParseResponseAsync<DocumentDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentDto>.Failure(result.Error!);

            return Result<DocumentDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved versjonsoppdatering av dokument {Slug}/{Id}", 
                documentTypeSlug, documentId);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved versjonsoppdatering av dokument {Slug}/{Id}", 
                documentTypeSlug, documentId);
            return Result<DocumentDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string documentTypeSlug, Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.DeleteAsync(ApiRoutes.Documents.ById(documentTypeSlug, id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av dokument {Slug}/{Id}", documentTypeSlug, id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av dokument {Slug}/{Id}", documentTypeSlug, id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}