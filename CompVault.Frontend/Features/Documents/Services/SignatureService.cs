using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public class SignatureService(
    ILogger<SignatureService> logger,
    IHttpClientFactory httpClientFactory) : ISignatureService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<List<UserSignatureStatusDto>>> GetSignaturesAsync(
        string documentTypeSlug, Guid documentId, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(ApiRoutes.Documents.Signatures(documentTypeSlug, documentId), ct);

            Result<List<UserSignatureStatusDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<UserSignatureStatusDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<UserSignatureStatusDto>>.Failure(result.Error!);

            return Result<List<UserSignatureStatusDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av signaturer for {Slug}/{Id}", 
                documentTypeSlug, documentId);
            return Result<List<UserSignatureStatusDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av signaturer for {Slug}/{Id}", 
                documentTypeSlug, documentId);
            return Result<List<UserSignatureStatusDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result> SignAsync(string documentTypeSlug, Guid documentId, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsync(ApiRoutes.Documents.Sign(documentTypeSlug, documentId), null, ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved signering av dokument {Slug}/{Id}", 
                documentTypeSlug, documentId);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved signering av dokument {Slug}/{Id}", 
                documentTypeSlug, documentId);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DocumentListDto>>> GetMySignedAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Documents.MySigned, ct);

            Result<List<DocumentListDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DocumentListDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DocumentListDto>>.Failure(result.Error!);

            return Result<List<DocumentListDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av signerte dokumenter");
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av signerte dokumenter");
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<DocumentListDto>>> GetMyPendingAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Documents.MyPending, ct);

            Result<List<DocumentListDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DocumentListDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DocumentListDto>>.Failure(result.Error!);

            return Result<List<DocumentListDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av ventende dokumenter");
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av ventende dokumenter");
            return Result<List<DocumentListDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}