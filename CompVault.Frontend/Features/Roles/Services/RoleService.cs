using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Roles;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Roles.Services;

public class RoleService(
    ILogger<RoleService> logger,
    IHttpClientFactory httpClientFactory) : IRoleService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);
    
    /// <inheritdoc />
    public async Task<Result<List<RoleDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Role.Base, ct);

            Result<List<RoleDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<RoleDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<RoleDto>>.Failure(result.Error!);

            return Result<List<RoleDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av roller");
            return Result<List<RoleDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av roller");
            return Result<List<RoleDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<RoleDto?>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Role.ById(id), ct);

            Result<RoleDto?> result =
                await HttpClientExtensions.ParseResponseAsync<RoleDto?>(response, ct);

            if (result.IsFailure)
                return Result<RoleDto?>.Failure(result.Error!);
            
            return Result<RoleDto?>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av rolle {RoleId}", id);
            return Result<RoleDto?>.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av rolle {RoleId}", id);
            return Result<RoleDto?>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PostAsJsonAsync(ApiRoutes.Role.Base, request, ct);

            Result<RoleDto> result = await HttpClientExtensions.ParseResponseAsync<RoleDto>(response, ct);

            if (result.IsFailure)
                return Result<RoleDto>.Failure(result.Error!);

            return Result<RoleDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppretting av rolle");
            return Result<RoleDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppretting av rolle");
            return Result<RoleDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    
    /// <inheritdoc />
    public async Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PutAsJsonAsync(ApiRoutes.Role.ById(id), request, ct);

            Result<RoleDto> result = await HttpClientExtensions.ParseResponseAsync<RoleDto>(response, ct);

            if (result.IsFailure)
                return Result<RoleDto>.Failure(result.Error!);

            return Result<RoleDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved oppdatering av rolle {Id}", id);
            return Result<RoleDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved oppdatering av rolle {Id}", id);
            return Result<RoleDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
 
    
    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(ApiRoutes.Role.ById(id), ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved sletting av rolle {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved sletting av rolle {Id}", id);
            return Result.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<RoleDto>> AssignPermissionsAsync(Guid id, AssignPermissionsRequest request, 
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = 
                await _httpClient.PutAsJsonAsync(ApiRoutes.Role.Permissions(id), request, ct);

            Result<RoleDto> result = await HttpClientExtensions.ParseResponseAsync<RoleDto>(response, ct);

            if (result.IsFailure)
                return Result<RoleDto>.Failure(result.Error!);

            return Result<RoleDto>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved tilknytting av permissions til rolle {RoleId}", id);
            return Result<RoleDto>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved tilknytting av permissions til rolle {RoleId}", id);
            return Result<RoleDto>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<List<ExpiringCompetencyDto>>> GetAllPermissionsAsync(CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiRoutes.Role.AllPermissions, ct);

            Result<List<ExpiringCompetencyDto>> result =
                await HttpClientExtensions.ParseResponseAsync<List<ExpiringCompetencyDto>>(response, ct);

            if (result.IsFailure)
                return Result<List<ExpiringCompetencyDto>>.Failure(result.Error!);

            return Result<List<ExpiringCompetencyDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av alle permissions");
            return Result<List<ExpiringCompetencyDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av permissions");
            return Result<List<ExpiringCompetencyDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }
}