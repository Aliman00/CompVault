using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Departments.Controllers;

/// <summary>
/// Avdelingsadministrasjon — hent, opprett, oppdater og slett avdelinger.
/// Krever at man er innlogget.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class DepartmentsController(IDepartmentService departmentService) : BaseController
{
    /// <summary>Henter alle aktive avdelinger.</summary>
    /// <response code="200">Liste med avdelinger.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<DepartmentDto>> result = await departmentService.GetAllAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter én avdeling basert på ID.</summary>
    /// <response code="200">Avdeling funnet.</response>
    /// <response code="404">Ingen avdeling med den ID-en.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<DepartmentDto> result = await departmentService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny avdeling.</summary>
    /// <response code="201">Avdeling opprettet.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Overordnet avdeling ble ikke funnet.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentDto>> CreateAsync(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        Result<DepartmentDto> result = await departmentService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Oppdaterer en eksisterende avdeling.</summary>
    /// <response code="200">Avdeling oppdatert.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Ingen avdeling med den ID-en.</response>
    /// <response code="409">Sirkulær referanse oppdaget.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        Result<DepartmentDto> result = await departmentService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Soft-sletter en avdeling.</summary>
    /// <response code="204">Avdeling slettet.</response>
    /// <response code="404">Ingen avdeling med den ID-en.</response>
    /// <response code="409">Avdelingen har underavdelinger eller medlemmer.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await departmentService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }
}