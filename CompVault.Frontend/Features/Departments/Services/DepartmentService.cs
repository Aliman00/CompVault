using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Departments.Services;

public class DepartmentService(
    ILogger<DepartmentService> logger,
    IHttpClientFactory httpClientFactory) : IDepartmentService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);
    
    /// <inheritdoc />
    public async Task<Result<List<DepartmentDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Department.Base, ct);

            Result<List<DepartmentDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<DepartmentDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<DepartmentDto>>.Failure(result.Error!);

            return Result<List<DepartmentDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av avdelinger");
            return Result<List<DepartmentDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av avdelinger");
            return Result<List<DepartmentDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}