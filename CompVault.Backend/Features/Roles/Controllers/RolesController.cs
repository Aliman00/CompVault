using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Roles.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Roles;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Roles.Controllers;

/// <summary>
/// Administrasjon av roller og permissions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.RolesRead)]
[Produces("application/json")]
public sealed class RolesController(IRoleService roleService) : BaseController
{
    /// <summary>Henter alle roller med tilhørende permissions.</summary>
    /// <response code="200">Liste med roller.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<RoleDto>> result = await roleService.GetAllAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter én rolle basert på ID.</summary>
    /// <response code="200">Rolle funnet.</response>
    /// <response code="404">Ingen rolle med den ID-en.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<RoleDto> result = await roleService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny rolle.</summary>
    /// <response code="201">Rolle opprettet.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="409">Rollenavnet finnes allerede.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> CreateAsync(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        Result<RoleDto> result = await roleService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Oppdaterer en eksisterende rolle.</summary>
    /// <response code="200">Rolle oppdatert.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Ingen rolle med den ID-en.</response>
    /// <response code="409">Rollenavnet finnes allerede.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        Result<RoleDto> result = await roleService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Sletter en rolle.</summary>
    /// <response code="204">Rolle slettet.</response>
    /// <response code="404">Ingen rolle med den ID-en.</response>
    /// <response code="409">Kan ikke slette systemrolle eller rolle med brukere.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.RolesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await roleService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }

    /// <summary>Tildeler permissions til en rolle (overskriver eksisterende).</summary>
    /// <response code="200">Permissions oppdatert.</response>
    /// <response code="400">Validering feilet eller ugyldig permission.</response>
    /// <response code="404">Ingen rolle med den ID-en.</response>
    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> AssignPermissionsAsync(
        Guid id,
        [FromBody] AssignPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        Result<RoleDto> result = await roleService.AssignPermissionsAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter alle tilgjengelige permissions.</summary>
    /// <response code="200">Liste med permissions.</response>
    [HttpGet("permissions")]
    [Authorize(Policy = Permissions.RolesWrite)]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetAllPermissionsAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<PermissionDto>> result = await roleService.GetAllPermissionsAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
}
