using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CompVault.Backend.Features.Auth;

/// <summary>
/// Implementerer passwordless autentisering med engangs-kode (OTP) og JWT.
/// </summary>
public sealed class AuthService(UserManager<ApplicationUser> userManager, IJwtService jwtService, IOptions<JwtSettings> jwtOptions) : IAuthService
{
    /// <inheritdoc />
    public async Task<Result<bool>> RequestOtpAsync(
        RequestOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        // Finn brukeren — men returner alltid suksess uansett utfall
        // for å unngå at angripere kan kartlegge hvilke e-poster som er registrert.
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);

        if (user is not null && user.IsActive && user.DeletedAt is null)
        {
            // TODO: Generer 6-sifret OTP-kode og lagre med utløpstid (f.eks. 10 min)
            // TODO: Send via valgt kanal:
            //   - OtpDeliveryMethod.Email → e-posttjeneste (SendGrid, SMTP o.l.)
            //   - OtpDeliveryMethod.Sms   → SMS-tjeneste (Twilio o.l.)
            // Kanal valgt av bruker: request.DeliveryMethod
            _ = request.DeliveryMethod; // brukes når utsending er implementert
        }

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse>> VerifyOtpAsync(
        VerifyOtpRequest request,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive || user.DeletedAt is not null)
            return Result<LoginResponse>.Failure(
                AppError.Create(ErrorCode.OtpInvalid, "Ugyldig eller utgatt kode."));

        // TODO: Hent lagret OTP for brukeren og verifiser:
        //   1. Koden matcher request.OtpCode
        //   2. Koden ikke er utgatt
        //   3. Merk koden som brukt (engangs)
        // Midlertidig: feiler alltid til OTP-lagring er implementert
        return Result<LoginResponse>.Failure(
            AppError.Create(ErrorCode.OtpInvalid, "OTP-verifisering er ikke implementert ennå."));
    }

    /// <inheritdoc />
    public async Task<Result<LoginResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        System.Security.Claims.ClaimsPrincipal? principal =
            jwtService.GetPrincipalFromExpiredToken(request.AccessToken);

        if (principal is null)
            return Result<LoginResponse>.Failure(
                AppError.Create(ErrorCode.InvalidToken, "Ugyldig access token."));

        string? userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? principal.FindFirst("sub")?.Value;

        if (userId is null || !Guid.TryParse(userId, out Guid parsedUserId))
            return Result<LoginResponse>.Failure(
                AppError.Create(ErrorCode.InvalidToken, "Ugyldige token-claims."));

        ApplicationUser? user = await userManager.FindByIdAsync(parsedUserId.ToString());

        if (user is null || !user.IsActive || user.DeletedAt is not null)
            return Result<LoginResponse>.Failure(
                AppError.Create(ErrorCode.InvalidToken, "Bruker ikke funnet eller inaktiv."));

        IList<string> roles = await userManager.GetRolesAsync(user);
        string newAccessToken = jwtService.GenerateAccessToken(user, roles);

        LoginResponse response = new()
        {
            AccessToken = newAccessToken,
            RefreshToken = jwtService.GenerateRefreshToken(),
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenMinutes),
            UserId = user.Id,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Roles = roles.ToList()
        };

        return Result<LoginResponse>.Success(response);
    }

    /// <inheritdoc />
    public Task<Result<bool>> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        // TODO: implementer token-familie-sporing når vi persister refresh tokens.
        return Task.FromResult(Result<bool>.Success(true));
    }
}
