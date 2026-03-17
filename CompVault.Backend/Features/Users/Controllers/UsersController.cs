using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Users.Services;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Users.Controllers;

/// <summary>
/// Brukeradministrasjon — hent, opprett, oppdater og slett brukere.
/// Krever at man er innlogget.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController(IUserService userService) : BaseController
{
    /// <summary>Henter alle aktive brukere.</summary>
    /// <response code="200">Liste med brukere.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<UserDto>> result = await userService.GetAllUsersAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter én bruker basert på ID.</summary>
    /// <response code="200">Bruker funnet.</response>
    /// <response code="404">Ingen bruker med den ID-en.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<UserDto> result = await userService.GetUserByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny brukerkonto.</summary>
    /// <response code="201">Bruker opprettet.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="409">E-posten er allerede i bruk.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> CreateAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserDto> result = await userService.CreateUserAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Oppdaterer profilen til en eksisterende bruker.</summary>
    /// <response code="200">Bruker oppdatert.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Ingen bruker med den ID-en.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        Result<UserDto> result = await userService.UpdateUserAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Soft-sletter brukeren — setter DeletedAt og deaktiverer kontoen.</summary>
    /// <response code="204">Bruker slettet.</response>
    /// <response code="404">Ingen bruker med den ID-en.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await userService.DeleteUserAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }
}
