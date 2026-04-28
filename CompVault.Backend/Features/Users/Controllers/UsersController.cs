using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.JobTitles.Services;
using CompVault.Backend.Features.Users.Services;
using CompVault.Backend.Infrastructure.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.JobTitles;
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
[Produces("application/json")]
public sealed class UsersController(
    IUserService userService,
    IJobTitleService jobTitleService) : BaseController
{
    /// <summary>Henter paginerte aktive brukere.</summary>
    /// <response code="200">Paginert liste med brukere.</response>
    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAllAsync(
        [FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        Result<PagedResult<UserDto>> result = await userService.GetAllUsersAsync(query, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter én bruker basert på ID.</summary>
    /// <response code="200">Bruker funnet.</response>
    /// <response code="404">Ingen bruker med den ID-en.</response>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.UsersRead)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<UserDto> result = await userService.GetUserByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
    
    /// <summary>
    /// Lar en bruker slå opp alle brukerne de har tilattelse til. Så fremt brukeren har riktig
    /// tilattelse. Frontend velger hvilke permissions som er påkrevd til de forskjellige featurene.
    /// Eks: Ved utlevering av utstyr så må brukeren har equipment:read for å kunne se brukere i egen avdeling,
    /// equipment:subread for å brukere i underavdelinger og equipment.readall for å se alle brukere
    /// </summary>
    [Authorize]
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(IReadOnlyList<UserLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserLookupDto>>> LookupAllowedUsers(
        [FromQuery] string readPermission,
        [FromQuery] string bypassPermission,
        [FromQuery] string subPermission,
        CancellationToken ct)
    {
        if (!User.HasPermission(readPermission))
            return Forbid();

        if (!User.HasPermission(bypassPermission) && !User.HasPermission(subPermission))
            return Forbid();

        Result<IReadOnlyList<UserLookupDto>> result =
            await userService.LookupAllowedUsersAsync(bypassPermission, subPermission, ct);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter alle aktive stillingstitler for autocomplete.</summary>
    /// <response code="200">Liste med stillingstitler.</response>
    [HttpGet("job-titles")]
    [Authorize(Policy = Permissions.UsersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<JobTitleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobTitleDto>>> GetJobTitlesAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<JobTitleDto>> result = await jobTitleService.GetAllAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>
    /// Henter alle brukere med leder-stillingstittel (IsLeader=true).
    /// Brukes i frontend som dropdown for å velge brukers nærmeste leder (ManagerId).
    /// </summary>
    /// <response code="200">Liste med potensielle ledere.</response>
    [HttpGet("managers")]
    [Authorize(Policy = Permissions.UsersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetPotentialManagersAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<UserDto>> result = await userService.GetPotentialManagersAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny brukerkonto.</summary>
    /// <response code="201">Bruker opprettet.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="409">E-posten er allerede i bruk.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.UsersWrite)]
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

        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
        // Merk: Bruker "GetById" som action-navn, ikke "GetByIdAsync", fordi ASP.NET Core
        // som default stripper "Async" fra action-navn i route tabellen.
    }

    /// <summary>Oppdaterer profilen til en eksisterende bruker.</summary>
    /// <response code="200">Bruker oppdatert.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Ingen bruker med den ID-en.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UsersWrite)]
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
    [Authorize(Policy = Permissions.UsersDelete)]
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