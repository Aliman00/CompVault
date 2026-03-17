using System.Diagnostics;
using CompVault.Backend.Common.Security;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Templates;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CompVault.Backend.Features.Auth.Services;

/// <summary>
/// Implementerer passwordless autentisering med engangs-kode (OTP) og JWT.
/// </summary>
public sealed class AuthService(
    UserManager<ApplicationUser> userManager, 
    ILogger<IAuthService> logger,
    IJwtService jwtService, 
    IRefreshTokenService refreshTokenService,
    IOtpCodeService otpCodeService,
    IEmailService emailService,
    IOptions<OtpOptions> otpOptions,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork) : IAuthService
{
    private readonly OtpOptions _otp = otpOptions.Value;

    /// <inheritdoc />
    public async Task<Result> RequestOtpAsync(RequestOtpRequest request, CancellationToken ct = default)
    {
        // Starter en stopwatch for å bruke like lang tid uansett
        var sw = Stopwatch.StartNew();

        try
        {
            // Finn brukeren — returner suksess uansett utfall med unntak av interne feil (f.eks. e-postleveringsfeil)
            // for å unngå at angripere kan kartlegge hvilke e-poster som er registrert.
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                logger.LogWarning("OTP request for {Reason}. Email: {Email}",
                    user == null ? "unknown account" : "inactive account",
                    request.Email);
                return Result.Success(); // returnerer Success for å unngå epostkartlegging
            }
            
            // Generer kode — servicen håndterer om brukeren er null eller ikke. Kun send epost hvis suksess
            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var codeResult = await otpCodeService.GenerateOtpCodeAsync(user.Id, ct);
                if (codeResult.IsFailure)
                    return Result.Success(); // returnerer Success for å unngå epostkartlegging

                Result deliverCodeResult;
                if (request.DeliveryMethod == OtpDeliveryMethod.Email)
                {
                    // Oppretter en EmailBody med ferdig template
                    var emailBody = EmailTemplates.OtpCode(codeResult.Value!);

                    // Sender epost og sjekker at det er ingen feil med epost sending
                    deliverCodeResult = await emailService.SendAsync(request.Email, emailBody, ct);
                    if (deliverCodeResult.IsFailure)
                    {
                        // Skjer det en uventet feil så vil frontend få en melding om det. Skal ikke skje
                        // i produksjon. Denne returnen bryr seg ikke om stopwatch
                        logger.LogError("OTP delivery failed for UserId: {UserId}", user.Id);
                        return Result.Failure(deliverCodeResult.Error!);
                    }
                }
                // else
                //     deliverCodeResult = await smsService.SendAsync();
                
                return Result.Success();
            }, ct);
        }
        finally
        {
            // Avslutter stopwatchen. Vi setter en delay uansett slik at metoden bruker en minimum tid (Dette må
            // testest grunding for å justere til riktig tid)
            await TimingGuard.EnforceMinimumTimeAsync(sw, _otp.MinResponseTimeRequestOtpMs, ct);
        }
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        // Starter en stopwatch for å bruke like lang tid uansett
        var sw = Stopwatch.StartNew();

        try
        {
            // Henter brukeren for å sjekke om e-posten er korrekt. Returner ingen feilmeldinger, kun logger
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
                logger.LogWarning("OTP-verification attempted for unknown email. Email: {Email}", request.Email);
            else if (!user.IsActive)
                logger.LogWarning("OTP-verification attempted for deactivated account. Email: {Email}", request.Email);


            // Samme feilmelding om brukeren eksisterer eller samme kode
            // Hvis grensen på forsøk er nådd, så får sender vi egen feilmelding til frontend
            if (user == null || !user.IsActive || user.DeletedAt != null)
                return Result<LoginResponse>.Failure(
                    AppError.Create(ErrorCode.OtpInvalidOrExpired, "Invalid or expired code"));
            
            // Verifiserer OTP og markerer koden som brukt
            var otpResult = await otpCodeService.VerifyOtpCodeAsync(user.Id, request.OtpCode, ct);
            if (otpResult.IsFailure)
                return Result<LoginResponse>.Failure(otpResult.Error!);
            
            // Oppretter en transaksjon som rollbacker eller lagrer til slutt
            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Koden er korrekt - Setter Otp-koden til brukt
                otpResult.Value!.IsUsed = true;

                // Opprett og lagre refresh token
                var refreshResult = await refreshTokenService.CreateRefreshTokenAsync(user.Id, ct);
                if (refreshResult.IsFailure)
                    return Result<LoginResponse>.Failure(refreshResult.Error!);

                // Henter roller for å bygge response og tokens
                var roles = await userManager.GetRolesAsync(user);

                // Generer tokens
                string accessToken = jwtService.GenerateAccessToken(user, roles);
                
                // Bygger LoginResponse med Access token, refresh token og annen nødvendig informasjon frontend trenger
                return Result<LoginResponse>.Success(new LoginResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshResult.Value!,
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Roles = roles.ToList()
                });
            }, ct);
        }
        finally
        {
            // Avslutter stopwatchen. Vi setter en delay uansett slik at metoden bruker en minimum tid (Dette må
            // testest grunding for å justere til riktig tid)
            await TimingGuard.EnforceMinimumTimeAsync(sw, _otp.MinResponseTimeVerifyOtpMs, ct);
        }
    }

    /// <inheritdoc />
    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request,
        CancellationToken ct = default)
    {
        // Henter og validerer refresh token fra databasen — tidligere ble dette ikke sjekket mot DB
        RefreshToken? storedToken = await refreshTokenRepository
            .GetValidTokenAsync(request.RefreshToken, ct);

        if (storedToken is null)
            return Result<RefreshTokenResponse>.Failure(
                AppError.Create(ErrorCode.InvalidToken, "Ugyldig eller utgått refresh token."));

        ApplicationUser? user = await userManager.FindByIdAsync(storedToken.UserId.ToString());

        if (user is null || !user.IsActive || user.DeletedAt is not null)
            return Result<RefreshTokenResponse>.Failure(
                AppError.Create(ErrorCode.InvalidToken, "Bruker ikke funnet eller inaktiv."));
        
        IList<string> roles = await userManager.GetRolesAsync(user);
        
        // Utføerer oppdatering og opprettelse i en transaksjon
        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Token rotation — revoker det gamle tokenet og utsteder et nytt.
            // Dette sikrer at hvert refresh token kun kan brukes én gang, og at
            // stjålne tokens oppdages ved neste forsøk på bruk.
            storedToken.IsRevoked = true;

            // Opprett og lagre refresh token
            var refreshResult = await refreshTokenService.CreateRefreshTokenAsync(user.Id, ct);
            if (refreshResult.IsFailure)
                return Result<RefreshTokenResponse>.Failure(refreshResult.Error!);

            return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse
            {
                AccessToken = jwtService.GenerateAccessToken(user, roles),
                RefreshToken = refreshResult.Value!,
            });
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // Henter tokenet fra databasen — kun gyldige tokens kan revokers
        RefreshToken? storedToken = await refreshTokenRepository
            .GetValidTokenAsync(refreshToken, ct);

        if (storedToken is null)
            return Result.Failure(
                AppError.Create(ErrorCode.InvalidToken, "Ugyldig eller utgått refresh token."));

        // Markerer tokenet som revokert — dette logger brukeren effektivt ut
        return await unitOfWork.ExecuteInTransactionAsync( () =>
        {
            storedToken.IsRevoked = true;
            return Task.FromResult(Result.Success());
        }, ct);
    }
}
