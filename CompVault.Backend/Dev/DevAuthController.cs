using CompVault.Backend.Common.Security;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Shared.DTOs.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Dev;

/// <summary>
/// Utvikler-innlogging med e-post og passord.
/// ADVARSEL: Denne kontrolleren eksisterer KUN for å gjøre testing enklere i Development-miljøet.
/// Den skal IKKE være tilgjengelig i produksjon — fjern Dev/-mappen og seed-kallet i Program.cs før deploy.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class DevAuthController(
    UserManager<ApplicationUser> userManager,
    IJwtService jwtService,
    IHostEnvironment env,
    IRefreshTokenService refreshTokenService,
    IOtpCodeRepository otpCodeRepository) : ControllerBase
{
    /// <summary>
    /// Logger inn med e-post og passord. Returnerer JWT identisk med OTP-flyten.
    /// Kun tilgjengelig i Development-miljøet.
    /// </summary>
    [HttpPost("dev-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> DevLoginAsync(
        [FromBody] DevLoginRequest request)
    {
        if (!env.IsDevelopment())
            return NotFound();

        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Ugyldig e-post eller passord." });

        bool passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Unauthorized(new { message = "Ugyldig e-post eller passord." });

        if (!user.IsActive || user.DeletedAt is not null)
            return Unauthorized(new { message = "Kontoen er deaktivert." });

        IList<string> roles = await userManager.GetRolesAsync(user);
        string accessToken = jwtService.GenerateAccessToken(user, roles);
        string refreshToken = refreshTokenService.GenerateRefreshToken();

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        });
    }
    
    /// <summary>
    /// Oppretter en Otp-code med 123456 slik at frontend kan kalle på VerifyOtp med kode 123456 og hoppe over
    /// sending av epost. Token og cookie opprettes korrekt
    /// </summary>
    [HttpPost("dev-create-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DevCreateOtpAsync([FromBody] RequestOtpRequest request)
    {
        if (!env.IsDevelopment())
            return NotFound();
        
        ApplicationUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(new { message = "Ugyldig e-post eller passord." });
        
        var otpCode = new OtpCode
        {
            UserId = user.Id,
            Code = OtpHasher.HashCode("123456"), // Hasher koden for lagring
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        };
        
        await otpCodeRepository.AddAsync(otpCode);
        await otpCodeRepository.SaveChangesAsync(); 

        return Ok();
    }
}