using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Infrastructure.Auth;
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
    IRefreshTokenService refreshTokenService) : ControllerBase
{
    /// <summary>
    /// Logger inn med e-post og passord. Returnerer JWT identisk med OTP-flyten.
    /// Kun tilgjengelig i Development-miljøet.
    /// </summary>
    [HttpPost("dev-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> DevLoginAsync(
        [FromBody] DevLoginRequest request,
        CancellationToken ct)
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

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Roles = roles.ToList()
        });
    }
}
