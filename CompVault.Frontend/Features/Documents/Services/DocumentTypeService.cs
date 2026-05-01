using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public class DocumentTypeService(
    ILogger<DocumentTypeService> logger,
    IHttpClientFactory httpClientFactory) : IDocumentTypeService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<DocumentTypeDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.DocumentTypes.Base, ct);

            Result<List<DocumentTypeDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DocumentTypeDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DocumentTypeDto>>.Failure(result.Error!);

            return Result<List<DocumentTypeDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av alle dokumenttyper");
            return Result<List<DocumentTypeDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av alle dokumenttyper");
            return Result<List<DocumentTypeDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeDto>> GetBySlugAsync(string slug, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.DocumentTypes.BySlug(slug), ct);

            Result<DocumentTypeDto> result =
                await HttpClientExtensions.ParseResponseAsync<DocumentTypeDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentTypeDto>.Failure(result.Error!);

            return Result<DocumentTypeDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av dokumenttype {Slug}", slug);
            return Result<DocumentTypeDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av dokumenttype {Slug}", slug);
            return Result<DocumentTypeDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<UserDocumentTypeDto>>> GetMyDocumentTypesAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.DocumentTypes.My, ct);

            Result<List<UserDocumentTypeDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<UserDocumentTypeDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<UserDocumentTypeDto>>.Failure(result.Error!);

            return Result<List<UserDocumentTypeDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av dokumenttyper for bruker");
            return Result<List<UserDocumentTypeDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av dokumenttyper for bruker");
            return Result<List<UserDocumentTypeDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeDto>> CreateAsync(CreateDocumentTypeRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.DocumentTypes.Base, request, ct);

            Result<DocumentTypeDto> result =
                await HttpClientExtensions.ParseResponseAsync<DocumentTypeDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentTypeDto>.Failure(result.Error!);

            return Result<DocumentTypeDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av dokumenttype");
            return Result<DocumentTypeDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av dokumenttype");
            return Result<DocumentTypeDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DocumentTypeDto>> UpdateAsync(string slug, UpdateDocumentTypeRequest request,
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PutAsJsonAsync(ApiRoutes.DocumentTypes.BySlug(slug), request, ct);

            Result<DocumentTypeDto> result =
                await HttpClientExtensions.ParseResponseAsync<DocumentTypeDto>(response, ct);

            if (result.IsFailure)
                return Result<DocumentTypeDto>.Failure(result.Error!);

            return Result<DocumentTypeDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av dokumenttype {Slug}", slug);
            return Result<DocumentTypeDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av dokumenttype {Slug}", slug);
            return Result<DocumentTypeDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(string slug, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.DeleteAsync(ApiRoutes.DocumentTypes.BySlug(slug), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av dokumenttype {Slug}", slug);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av dokumenttype {Slug}", slug);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}