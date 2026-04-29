using CompVault.Backend.Common.Controller;
using CompVault.Backend.Common.Security;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Features.Users;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    IOtpCodeRepository otpCodeRepository,
    IPermissionService permissionService,
    AppDbContext dbContext) : BaseController
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
    public async Task<ActionResult<TokenResponse>> DevLoginAsync([FromBody] DevLoginRequest request)
    {
        if (!env.IsDevelopment())
            return NotFound();

        ApplicationUser? user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
            return Unauthorized(new { message = "Ugyldig e-post eller passord." });

        bool passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Unauthorized(new { message = "Ugyldig e-post eller passord." });

        if (!user.IsActive || user.DeletedAt is not null)
            return Unauthorized(new { message = "Kontoen er deaktivert." });

        IList<string> roles = await userManager.GetRolesAsync(user);
        IList<string> permissions = await permissionService.GetPermissionsForRolesAsync(roles, CancellationToken.None);
        string accessToken = jwtService.GenerateAccessToken(user, roles, permissions);
        Result<string> refreshResult = await refreshTokenService.CreateRefreshTokenAsync(user.Id, CancellationToken.None);

        if (refreshResult.IsFailure)
            return StatusCode(500, new { message = "Kunne ikke opprette refresh token." });

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshResult.Value!
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

        ApplicationUser? user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
            return Unauthorized(new { message = "Ugyldig e-post eller passord." });
        
        OtpCode? existing = await dbContext.OtpCodes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.UserId == user.Id);

        if (existing is not null)
        {
            dbContext.OtpCodes.Remove(existing);
            await dbContext.SaveChangesAsync(); // <- lagre slettingen før vi oppretter ny
        }

        var otpCode = new OtpCode
        {
            UserId = user.Id,
            Code = OtpHasher.HashCode("123456"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
        };

        await otpCodeRepository.AddAsync(otpCode);
        await otpCodeRepository.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("dev-get-users")]
    [ProducesResponseType(typeof(List<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<ApplicationUser> users = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.Department)
            .Include(u => u.Manager)
            .Include(u => u.JobTitle)
            .Where(u => u.IsActive && u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>();
        foreach (ApplicationUser user in users)
        {
            IList<string> roles = await userManager.GetRolesAsync(user);
            dtos.Add(UserMapper.ToDto(user, roles));
        }

        return Ok(dtos);
    }
}