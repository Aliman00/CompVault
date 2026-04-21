using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Equipment.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Equipment.Controllers;

/// <summary>
/// Administrasjon av utleveringer.
/// </summary>
[ApiController]
[Route(ApiRoutes.EquipmentIssuances.Base)]
[Authorize(Policy = Permissions.EquipmentRead)]
[Produces("application/json")]
public sealed class EquipmentIssuancesController(IEquipmentIssuanceService issuanceService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EquipmentIssuanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentIssuanceDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EquipmentIssuanceDto>> result =
            await issuanceService.GetAllAsync(cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EquipmentIssuanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentIssuanceDto>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Result<EquipmentIssuanceDto> result = await issuanceService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpGet("by-user/{userId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<EquipmentIssuanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentIssuanceDto>>> GetByUserAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EquipmentIssuanceDto>> result =
            await issuanceService.GetByUserAsync(userId, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EquipmentWrite)]
    [ProducesResponseType(typeof(EquipmentIssuanceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EquipmentIssuanceDto>> CreateAsync(
        [FromBody] CreateEquipmentIssuanceRequest request,
        CancellationToken cancellationToken)
    {
        Result<EquipmentIssuanceDto> result = await issuanceService.CreateAsync(request, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EquipmentWrite)]
    [ProducesResponseType(typeof(EquipmentIssuanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentIssuanceDto>> UpdateAsync(
        Guid id, [FromBody] UpdateEquipmentIssuanceRequest request,
        CancellationToken cancellationToken)
    {
        Result<EquipmentIssuanceDto> result = await issuanceService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.EquipmentDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await issuanceService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return NoContent();
    }
}