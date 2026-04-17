using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.CompetencyTypes;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Competencies.Services;

public class CompetencyTypeService(
    ILogger<CompetencyTypeService> logger,
    IHttpClientFactory httpClientFactory) : ICompetencyTypeService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);
    
    /// <inheritdoc />
    public async Task<Result<List<CompetencyTypeDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.GetAsync(ApiRoutes.CompetencyTypes.Base, ct);

            Result<List<CompetencyTypeDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<CompetencyTypeDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<CompetencyTypeDto>>.Failure(result.Error!);

            return Result<List<CompetencyTypeDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av kompetansetyper");
            return Result<List<CompetencyTypeDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av kompetansetyper");
            return Result<List<CompetencyTypeDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<CompetencyTypeDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.GetAsync(ApiRoutes.CompetencyTypes.ById(id), ct);

            Result<CompetencyTypeDto> result =
                await HttpClientExtensions.ParseResponseAsync<CompetencyTypeDto>(response, ct);

            if (result.IsFailure)
                return Result<CompetencyTypeDto>.Failure(result.Error!);
            
            return Result<CompetencyTypeDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av kompetansetyper {CompetencyTypeId}", id);
            return Result<CompetencyTypeDto>.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av kompetansetyper {CompetencyTypeId}", id);
            return Result<CompetencyTypeDto>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<CompetencyTypeDto>> CreateAsync(CreateCompetencyTypeRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PostAsJsonAsync(ApiRoutes.CompetencyTypes.Base, request, ct);

            Result<CompetencyTypeDto> result =
                await HttpClientExtensions.ParseResponseAsync<CompetencyTypeDto>(response, ct);

            if (result.IsFailure)
                return Result<CompetencyTypeDto>.Failure(result.Error!);

            return Result<CompetencyTypeDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av kompetansetyper");
            return Result<CompetencyTypeDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av kompetansetyper");
            return Result<CompetencyTypeDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<CompetencyTypeDto>> UpdateAsync(Guid id, UpdateCompetencyTypeRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PutAsJsonAsync(ApiRoutes.CompetencyTypes.ById(id), request, ct);

            Result<CompetencyTypeDto> result = 
                await HttpClientExtensions.ParseResponseAsync<CompetencyTypeDto>(response, ct);

            if (result.IsFailure)
                return Result<CompetencyTypeDto>.Failure(result.Error!);

            return Result<CompetencyTypeDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av kompetansetyper {Id}", id);
            return Result<CompetencyTypeDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av kompetansetyper {Id}", id);
            return Result<CompetencyTypeDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(
                ApiRoutes.CompetencyTypes.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av kompetansetyper {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av kompetansetyper {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}