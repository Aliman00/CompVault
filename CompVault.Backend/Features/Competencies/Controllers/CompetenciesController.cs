using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Competencies.Services;
using CompVault.Shared.DTOs.Competencies;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Competencies.Controllers;

/// <summary>
/// Administrasjon av kompetansebevis — hent, opprett, oppdater, slett
/// og hent utløpende/utløpte bevis.
/// Alle lesende operasjoner krever innlogging.
/// Skrivende operasjoner krever Admin-rolle.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public sealed class CompetenciesController(ICompetencyService competencyService) : BaseController
{
    /// <summary>Henter kompetansebevis med filtrering.</summary>
    /// <param name="userId">Valgfritt — filtrer på bruker-ID.</param>
    /// <param name="status">Valgfritt — filtrer på status.</param>
    /// <param name="competencyTypeId">Valgfritt — filtrer på kompetansetype-ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Liste med kompetansebevis.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompetencyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompetencyDto>>> GetAllAsync(
        [FromQuery] Guid? userId,
        [FromQuery] CompetencyStatus? status,
        [FromQuery] Guid? competencyTypeId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<CompetencyDto>> result = await competencyService.GetAllAsync(
            userId, status, competencyTypeId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter ett kompetansebevis basert på ID.</summary>
    /// <response code="200">Kompetansebevis funnet.</response>
    /// <response code="404">Ingen kompetansebevis med den ID-en.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompetencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompetencyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<CompetencyDto> result = await competencyService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter et nytt kompetansebevis.</summary>
    /// <response code="201">Kompetansebevis opprettet.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Bruker eller kompetansetype ble ikke funnet.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CompetencyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompetencyDto>> CreateAsync(
        [FromBody] CreateCompetencyRequest request,
        CancellationToken cancellationToken)
    {
        Result<CompetencyDto> result = await competencyService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        // IsSuccess garanterer at Value ikke er null per Result<T>-kontrakten
        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Oppdaterer et eksisterende kompetansebevis (inkl. revoke).</summary>
    /// <response code="200">Kompetansebevis oppdatert.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Ingen kompetansebevis med den ID-en.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CompetencyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompetencyDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateCompetencyRequest request,
        CancellationToken cancellationToken)
    {
        Result<CompetencyDto> result = await competencyService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Soft-sletter et kompetansebevis.</summary>
    /// <response code="204">Kompetansebevis slettet.</response>
    /// <response code="404">Ingen kompetansebevis med den ID-en.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await competencyService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }

    /// <summary>Henter utløpende og utløpte kompetansebevis.</summary>
    /// <param name="userId">Valgfritt — filtrer på bruker-ID.</param>
    /// <param name="departmentId">Valgfritt — filtrer på avdeling-ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Liste med utløpende/utløpte kompetansebevis.</response>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpiringCompetencyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExpiringCompetencyDto>>> GetExpiringAsync(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ExpiringCompetencyDto>> result = await competencyService.GetExpiringAsync(
            userId, departmentId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
}