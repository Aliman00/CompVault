using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Audit;

public class AuditService(
    ILogger<AuditService> logger,
    IHttpClientFactory httpClientFactory) : IAuditService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result<PagedResult<AuditLogDto>>> GetAsync(AuditLogQueryParameters parameters, CancellationToken ct)
    {
        try
        {
            string url = BuildQueryUrl(parameters);
            HttpResponseMessage response = await _httpClient.GetAsync(url, ct);

            Result<PagedResult<AuditLogDto>> result =
                await HttpClientExtensions.ParseResponseAsync<PagedResult<AuditLogDto>>(response, ct);

            if (result.IsFailure)
                return Result<PagedResult<AuditLogDto>>.Failure(result.Error!);

            return Result<PagedResult<AuditLogDto>>.Success(result.Value!);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved henting av revisjonslogg");
            return Result<PagedResult<AuditLogDto>>.Failure(AppError.Create(ErrorCode.NetworkError,
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved henting av revisjonslogg");
            return Result<PagedResult<AuditLogDto>>.Failure(AppError.Create(ErrorCode.Unknown,
                "Noe gikk galt. Prøv igjen."));
        }
    }

    private static string BuildQueryUrl(AuditLogQueryParameters p)
    {
        var queryParams = new Dictionary<string, string?>();

        if (!string.IsNullOrWhiteSpace(p.Action))
            queryParams["action"] = p.Action;
        if (!string.IsNullOrWhiteSpace(p.EntityType))
            queryParams["entityType"] = p.EntityType;
        if (p.EntityId.HasValue)
            queryParams["entityId"] = p.EntityId.ToString();
        if (p.UserId.HasValue)
            queryParams["userId"] = p.UserId.ToString();
        if (p.From.HasValue)
            queryParams["from"] = p.From.Value.ToString("O");
        if (p.To.HasValue)
            queryParams["to"] = p.To.Value.ToString("O");

        queryParams["page"] = p.Page.ToString();
        queryParams["pageSize"] = p.PageSize.ToString();

        return ApiRoutes.Audit.Base.AddQueryFilter(queryParams);
    }
}