using System.Text.Json;

using CompVault.Shared.Result;
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
            ProblemDetail? problemDetail = await response.Content.ReadFromJsonAsync<ProblemDetail>(ct);
            if (problemDetail == null)
                return AppError.Create(ErrorCode.Unknown, "Ukjent feil fra serveren");

            if (!Enum.TryParse(problemDetail.Code, out ErrorCode errorCode))
                errorCode = ErrorCode.Unknown;

            return AppError.Create(errorCode, problemDetail.Message);
        }
        catch (JsonException)
        {
            return AppError.Create(ErrorCode.Unknown, "Ukjent feil fra serveren");
        }
    }
}