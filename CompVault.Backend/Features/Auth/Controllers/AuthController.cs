using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Auth.Controllers;

/// <summary>
/// Håndterer passwordless innlogging via engangs-kode (OTP), token-refresh og utlogging.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : BaseController
{
    /// <summary>
    /// Steg 1: Sender en engangs-kode til brukeren via valgt kanal (e-post eller SMS).
    /// Returnerer alltid 200 OK uavhengig av om e-posten er registrert — for å unngå e-postkartlegging.
    /// </summary>
    /// <response code="200">Engangs-kode sendt (eller forespørsel behandlet).</response>
    [HttpPost(ApiRoutes.Auth.RequestOtp)]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestOtpAsync(
        [FromBody] RequestOtpRequest request,
        CancellationToken ct)
    {
        var result = await authService.RequestOtpAsync(request, ct);
        
        if (result.IsFailure)
            return HandleFailure(result);
        
        // Returnerer alltid 200 så fremt ingen interne feil — se IAuthService.RequestOtpAsync
        return Ok();
    }

    /// <summary>
    /// Steg 2: Verifiserer engangs-koden og returnerer et JWT token-par ved suksess.
    /// </summary>
    /// <response code="200">Innlogging vellykket.</response>
    /// <response code="401">Ugyldig eller utgått kode.</response>
    /// <response code="429">For mange forsøk eller cooldown aktiv</response>
    [HttpPost(ApiRoutes.Auth.VerifyOtp)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> VerifyOtpAsync(
        [FromBody] VerifyOtpRequest request,
        CancellationToken ct)
    {
        var result = await authService.VerifyOtpAsync(request, ct);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter et nytt access token ved hjelp av refresh token.</summary>
    /// <response code="200">Nytt token utstedt.</response>
    /// <response code="401">Ugyldig eller utgått token.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> RefreshTokenAsync(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Ugyldiggjør refresh token og logger brukeren ut.</summary>
    /// <response code="204">Token ugyldiggjort.</response>
    /// <response code="401">Ikke innlogget.</response>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAsync(
        [FromBody] string refreshToken,
        CancellationToken cancellationToken)
    {
        await authService.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        return NoContent();
    }
}
