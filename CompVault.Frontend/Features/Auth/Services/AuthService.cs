using System.Security.Claims;
using System.Text.Json;
using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Auth.Services;

public class AuthService(
    ILogger<AuthService> logger, 
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor  httpContextAccessor) : IAuthService
{
    /// <summary>
    /// HttpClient mot backend
    /// </summary>
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(BackendApiSettings.MainClientName);

    /// <inheritdoc />
    public async Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Request OTP: {@Payload}", request);

            // Sender Http-forespørselen med requesten
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.RequestOtpFull, 
                request, ct);

            return await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved OTP-forespørsel for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved OTP-forespørsel for {Email}", request.Email);
            return Result.Failure(AppError.Create(ErrorCode.Unknown, "Noe gikk galt. Prøv igjen."));
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<(ClaimsPrincipal, TokenResponse)>> VerifyOtpAsync(VerifyOtpRequest request, 
        CancellationToken ct)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(ApiRoutes.Auth.VerifyOtpFull, request, ct);

            Result<TokenResponse> tokenResult =
                await HttpClientExtensions.ParseResponseAsync<TokenResponse>(response, ct);

            if (tokenResult.IsFailure)
                return Result<(ClaimsPrincipal, TokenResponse)>.Failure(tokenResult.Error!);
            
            // Oppretter en ClaimsPrincipal med alle claimene som vi bruker til å sette cookie i nettleseren
            IEnumerable<Claim> claims = ParseClaimsFromJwt(tokenResult.Value!.AccessToken);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            
            return Result<(ClaimsPrincipal, TokenResponse)>.Success((principal, tokenResult.Value!));
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Nettverksfeil ved OTP-verifisering for {Email}", request.Email);
            return Result<(ClaimsPrincipal, TokenResponse)>.Failure(AppError.Create(ErrorCode.NetworkError, 
                "Tilkoblingen feilet. Sjekk nettverket ditt."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet feil ved OTP-verifisering for {Email}", request.Email);
            return Result<(ClaimsPrincipal, TokenResponse)>.Failure(AppError.Create(ErrorCode.Unknown, 
                "Noe gikk galt. Prøv igjen."));
        }
    }
    
    
    /// <inheritdoc />
    public async Task LogOutAsync(CancellationToken ct)
    {
        try
        {
            string? refreshToken = httpContextAccessor.HttpContext?.Request.Cookies["refreshToken"];

            // Sender refresh token i body — ingen manuell Cookie-header
            var revokeRequest = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Auth.RevokeFull)
            {
                Content = JsonContent.Create(new RefreshTokenRequest{ RefreshToken = refreshToken ?? "" })
            };
            
            HttpResponseMessage response = await _httpClient.SendAsync(revokeRequest, ct);
            Result revokeResult = await HttpClientExtensions.ParseEmptyResponseAsync(response, ct);

            if (revokeResult.IsFailure)
                logger.LogWarning("Token-revokering feilet: [{ErrorCode}] {Message}",
                    revokeResult.Error!.Code, revokeResult.Error.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uventet  feil ved utlogging");
        }
    }
    
    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        // JWT er 3 stk base64url-segmenter som separeres med en punkt - vi skal ha midterste som blir kalt payload
        string base64UrlPayload = jwt.Split('.')[1];
    
        // Konverterer til vanlig base64 og fjerner padding som base64url fjernet
        // Vi må gjøre dette for at Conver.FromBase64String skal kunne det om til json
        string standardBase64 = base64UrlPayload.Replace('-', '+').Replace('_', '/');
        string paddedBase64 = standardBase64.PadRight(standardBase64.Length + 
                                                      (4 - standardBase64.Length % 4) % 4, '=');
        // Base64 gjøres om til JSON for å kunne deserialiseres, slik at vi får en ordbok med verdiene fra JWT-en
        string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(paddedBase64));
        Dictionary<string, JsonElement>? parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
    
        // Vi returnerer en liste med Claims, hentet fra nøkkelparet i ordboka. Vi har lagt til at vi håndterer claims
        // som er arrays (feks Roller)
        var claims = new List<Claim>();
        foreach (KeyValuePair<string, JsonElement> kv in parsed)
        {
            if (kv.Value.ValueKind == JsonValueKind.Array)
            {
                claims.AddRange(kv.Value.EnumerateArray()
                    .Select(element => new Claim(kv.Key, element.ToString())));
            }
            else
            {
                claims.Add(new Claim(kv.Key, kv.Value.ToString()));
            }
        }
    
        return claims;
    }


}