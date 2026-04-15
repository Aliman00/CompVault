using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Result;
using ExpiringCompetencyDto = CompVault.Shared.DTOs.Competencies.ExpiringCompetencyDto;
namespace CompVault.Frontend.Features.Competencies.Services;

public class CompetencyService(
    ILogger<CompetencyService> logger,
    IHttpClientFactory httpClientFactory) : ICompetencyService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);
    
    /// <inheritdoc />
    public async Task<Result<List<CompetencyDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Competencies.Base, ct);

            Result<List<CompetencyDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<CompetencyDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<CompetencyDto>>.Failure(result.Error!);

            return Result<List<CompetencyDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av kompetanser");
            return Result<List<CompetencyDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av kompetanser");
            return Result<List<CompetencyDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<CompetencyDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Competencies.ById(id), ct);

            Result<CompetencyDto> result =
                await HttpClientExtensions.ParseResponseAsync<CompetencyDto>(response, ct);

            if (result.IsFailure)
                return Result<CompetencyDto>.Failure(result.Error!);
            
            return Result<CompetencyDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av kompetanser {CompetencyId}", id);
            return Result<CompetencyDto>.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av kompetanser {CompetencyId}", id);
            return Result<CompetencyDto>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<CompetencyDto>> CreateAsync(CreateCompetencyRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PostAsJsonAsync(ApiRoutes.Competencies.Base, request, ct);

            Result<CompetencyDto> result = await HttpClientExtensions.ParseResponseAsync<CompetencyDto>(response, ct);

            if (result.IsFailure)
                return Result<CompetencyDto>.Failure(result.Error!);

            return Result<CompetencyDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av kompetanser");
            return Result<CompetencyDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av kompetanser");
            return Result<CompetencyDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<CompetencyDto>> UpdateAsync(Guid id, UpdateCompetencyRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PutAsJsonAsync(ApiRoutes.Competencies.ById(id), request, ct);

            Result<CompetencyDto> result = await HttpClientExtensions.ParseResponseAsync<CompetencyDto>(response, ct);

            if (result.IsFailure)
                return Result<CompetencyDto>.Failure(result.Error!);

            return Result<CompetencyDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av kompetanser {Id}", id);
            return Result<CompetencyDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av kompetanser {Id}", id);
            return Result<CompetencyDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(ApiRoutes.Competencies.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av kompetanser {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av kompetanser {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<List<ExpiringCompetencyDto>>> GetExpiringAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Competencies.Expiring, ct);

            Result<List<ExpiringCompetencyDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<ExpiringCompetencyDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<ExpiringCompetencyDto>>.Failure(result.Error!);

            return Result<List<ExpiringCompetencyDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av alle utgåtte kompetansebevis");
            return Result<List<ExpiringCompetencyDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av alle utgåtte kompetansebevis");
            return Result<List<ExpiringCompetencyDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}