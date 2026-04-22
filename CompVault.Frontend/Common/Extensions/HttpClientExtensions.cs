using System.Text.Json;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Models;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.WebUtilities;
namespace CompVault.Frontend.Common.Extensions;


/// <summary>
/// Extensions metoder for å enkelt kunne lese response fra backend og gjøre det om til Result eller ProblemDetail
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Sjekker at endepunkter som returnerer tom 200 OK er vellykket eller så henter den ut ProblemDetail-objektet
    /// fra responsen
    /// </summary>
    public static async Task<Result> ParseEmptyResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            return Result.Failure(await ReadProblemDetailAsync(response, ct));

        return Result.Success();
    }

    /// <summary>
    /// Sjekker at endepunkter som returnerer verdi er vellykket eller så henter den ut ProblemDetail-objektet
    /// fra responsen
    /// </summary>
    public static async Task<Result<T>> ParseResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            return Result<T>.Failure(await ReadProblemDetailAsync(response, ct));

        try
        {
            T? body = await response.Content.ReadFromJsonAsync<T>(ct);

            // Dette kan kun skje hvis backend sender "null"
            if (body == null)
                return Result<T>.Failure(AppError.Create(ErrorCode.Unknown,
                    "Server returnerte suksess og null i body"));

            return Result<T>.Success(body);
        }
        catch (JsonException)
        {
            return Result<T>.Failure(AppError.Create(ErrorCode.Unknown,
                "Kunne ikke lese JSON-respons fra server"));
        }
    }

    // Henter vårt ProblemDetail-objekt fra en error-response fra backend. Noe er galt, så får den Defaulte verdier
    private static async Task<AppError> ReadProblemDetailAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            // Henter ut hele bodyen som en et JsonDocument for å sjekke om det er datavalidation eller vår egen
            // feilmelding
            using JsonDocument doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), 
                cancellationToken: ct);
            JsonElement root = doc.RootElement;

            // Vi sjekker om det er et errors-felt her i responsen. Er det et så er det da DataValidation
            if (root.TryGetProperty("errors", out JsonElement errors))
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(field => field.Value.EnumerateArray()
                        .Select(v => v.GetString() ?? string.Empty))
                    .Where(message => !string.IsNullOrEmpty(message))
                    .ToList();

                string combined = string.Join(" ", messages);
                return AppError.Create(ErrorCode.Validation, combined);
            }

            // Henter ut code og message og mapper til en AppError for å vise i UI-en
            string? code = root.TryGetProperty("code", out JsonElement c) ? c.GetString() : null;
            string? message = root.TryGetProperty("message", out JsonElement m) ? m.GetString() : null;

            if (!Enum.TryParse(code, out ErrorCode errorCode))
                errorCode = ErrorCode.Unknown;

            return AppError.Create(errorCode, message ?? "Ukjent feil fra serveren");
        }
        catch (JsonException)
        {
            return AppError.Create(ErrorCode.Unknown, "Ukjent feil fra serveren");
        }
    }

    /// <summary>
    /// Bygger query filter til en URL
    /// </summary>
    /// <param name="baseUrl">URL-en til endepunktet</param>
    /// <param name="queryParams">Ordbok for å bygge filterne</param>
    /// <returns>Kombinert url med query-filtering satt</returns>
    public static string AddQueryFilter(this string baseUrl, Dictionary<string, string?> queryParams)
    {
        return queryParams.Count == 0
            ? baseUrl
            : QueryHelpers.AddQueryString(baseUrl, queryParams);
    }
    
    /// <summary>
    /// Generisk metode som bygger og poster en MultipartFormDataContent med en request og
    /// eventuelt en fil hvis vedlagt.
    /// </summary>
    /// <param name="httpClient">Klienten vi sender med</param>
    /// <param name="url">URL-en til endepunktet</param>
    /// <param name="request">Generisk request. Eks: CraeteDocument-request</param>
    /// <param name="file">Valgrfitt vedlagt fil som FileAttachment</param>
    /// <param name="ct"></param>
    /// <typeparam name="TRequest">Generisk request. Eks: CraeteDocument-request</typeparam>
    /// <returns>Responsen fra backend som en HttpResponseMessage</returns>
    public static async Task<HttpResponseMessage> PostAsMultipartFormAsync<TRequest>(
        this HttpClient httpClient,
        string url,
        TRequest request,
        FileAttachment? file = null,
        CancellationToken ct = default)
        where TRequest : class
    {
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request, file);
        return await httpClient.PostAsync(url, content, ct);
    }
    
    
    /// <summary>
    /// Leser filinnholdet fra en HTTP-respons og mapper det til en FileAttachment
    /// </summary>
    /// <param name="response">Http-forespørsel fra backend</param>
    /// <param name="ct"></param>
    /// <returns>Ferdig mappet FileAttachment for nedlastning</returns>
    internal static async Task<FileAttachment> ReadFileAttachmentAsync(HttpResponseMessage response, 
        CancellationToken ct)
    {
        Stream stream = await response.Content.ReadAsStreamAsync(ct);
        string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                          ?? response.Content.Headers.ContentDisposition?.FileName
                          ?? "dokument"
                              .Trim('"');

        return new FileAttachment(stream, fileName, contentType);
    }
    
    
}