using CompVault.Backend.Features.Auth;
using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Controllers;

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
    [HttpPost("request-otp")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestOtpAsync(
        [FromBody] RequestOtpRequest request,
        CancellationToken cancellationToken)
    {
        await authService.RequestOtpAsync(request, cancellationToken);
        // Returnerer alltid 200 — se IAuthService.RequestOtpAsync
        return Ok();
    }

    /// <summary>
    /// Steg 2: Verifiserer engangs-koden og returnerer et JWT token-par ved suksess.
    /// </summary>
    /// <response code="200">Innlogging vellykket.</response>
    /// <response code="401">Ugyldig eller utgått kode.</response>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> VerifyOtpAsync(
        [FromBody] VerifyOtpRequest request,
        CancellationToken cancellationToken)
    {
        Result<LoginResponse> result = await authService.VerifyOtpAsync(request, cancellationToken);

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
        Result<LoginResponse> result = await authService.RefreshTokenAsync(request, cancellationToken);

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
