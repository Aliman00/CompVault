using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.JobTitles.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.JobTitles;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.JobTitles.Controllers;

/// <summary>
/// Stillingstittel-administrasjon — hent, opprett, oppdater og slett stillingstitler.
/// Krever at man er innlogget.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Permissions.JobTitlesRead)]
[Produces("application/json")]
public sealed class JobTitlesController(IJobTitleService jobTitleService) : BaseController
{
    /// <summary>Henter alle aktive stillingstitler.</summary>
    /// <response code="200">Liste med stillingstitler.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<JobTitleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JobTitleDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<JobTitleDto>> result = await jobTitleService.GetAllAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter én stillingstittel basert på ID.</summary>
    /// <response code="200">Stillingstittel funnet.</response>
    /// <response code="404">Ingen stillingstittel med den ID-en.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobTitleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobTitleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<JobTitleDto> result = await jobTitleService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny stillingstittel.</summary>
    /// <response code="201">Stillingstittel opprettet.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="409">Navn finnes allerede.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.JobTitlesWrite)]
    [ProducesResponseType(typeof(JobTitleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobTitleDto>> CreateAsync(
        [FromBody] CreateJobTitleRequest request,
        CancellationToken cancellationToken)
    {
        Result<JobTitleDto> result = await jobTitleService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Oppdaterer en eksisterende stillingstittel.</summary>
    /// <response code="200">Stillingstittel oppdatert.</response>
    /// <response code="400">Validering feilet.</response>
    /// <response code="404">Ingen stillingstittel med den ID-en.</response>
    /// <response code="409">Navn finnes allerede.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.JobTitlesWrite)]
    [ProducesResponseType(typeof(JobTitleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<JobTitleDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateJobTitleRequest request,
        CancellationToken cancellationToken)
    {
        Result<JobTitleDto> result = await jobTitleService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Soft-sletter en stillingstittel.</summary>
    /// <response code="204">Stillingstittel slettet.</response>
    /// <response code="404">Ingen stillingstittel med den ID-en.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.JobTitlesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await jobTitleService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }
}