using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Competencies.Services;
using CompVault.Shared.DTOs.CompetencyTypes;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Competencies.Controllers;

/// <summary>
/// Administrasjon av kompetansetyper — hent, opprett, oppdater og slett.
/// Alle lesende operasjoner krever innlogging.
/// Skrivende operasjoner krever Admin-rolle.
/// </summary>
[ApiController]
[Route("api/competency-types")]
[Authorize]
[Produces("application/json")]
public sealed class CompetencyTypesController(ICompetencyTypeService competencyTypeService) : BaseController
{
    /// <summary>Henter alle aktive kompetansetyper.</summary>
    /// <response code="200">Liste med kompetansetyper.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompetencyTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompetencyTypeDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CompetencyTypeDto>> result = await competencyTypeService.GetAllAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter én kompetansetype basert på ID.</summary>
    /// <response code="200">Kompetansetype funnet.</response>
    /// <response code="404">Ingen kompetansetype med den ID-en.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompetencyTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompetencyTypeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<CompetencyTypeDto> result = await competencyTypeService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny kompetansetype.</summary>
    /// <response code="201">Kompetansetype opprettet.</response>
    /// <response code="400">Validering feilet eller navn finnes allerede.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CompetencyTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompetencyTypeDto>> CreateAsync(
        [FromBody] CreateCompetencyTypeRequest request,
        CancellationToken cancellationToken)
    {
        Result<CompetencyTypeDto> result = await competencyTypeService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Oppdaterer en eksisterende kompetansetype.</summary>
    /// <response code="200">Kompetansetype oppdatert.</response>
    /// <response code="400">Validering feilet eller navn finnes allerede.</response>
    /// <response code="404">Ingen kompetansetype med den ID-en.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CompetencyTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompetencyTypeDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateCompetencyTypeRequest request,
        CancellationToken cancellationToken)
    {
        Result<CompetencyTypeDto> result = await competencyTypeService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Soft-sletter en kompetansetype.</summary>
    /// <response code="204">Kompetansetype slettet.</response>
    /// <response code="404">Ingen kompetansetype med den ID-en.</response>
    /// <response code="409">Kompetansetypen har aktive kompetansebevis.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await competencyTypeService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }
}
