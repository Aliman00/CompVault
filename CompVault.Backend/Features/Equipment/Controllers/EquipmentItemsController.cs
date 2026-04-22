using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Equipment.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Equipment.Controllers;

/// <summary>
/// Administrasjon av utstyr under kategorier.
/// </summary>
[ApiController]
[Route(ApiRoutes.EquipmentItems.Base)]
[Authorize(Policy = Permissions.EquipmentRead)]
[Produces("application/json")]
public sealed class EquipmentItemsController(IEquipmentItemService itemService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EquipmentItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentItemDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EquipmentItemDto>> result = await itemService.GetAllAsync(cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EquipmentItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentItemDto>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Result<EquipmentItemDto> result = await itemService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<EquipmentItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentItemDto>>> GetByCategoryAsync(
        Guid categoryId, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EquipmentItemDto>> result =
            await itemService.GetByCategoryAsync(categoryId, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EquipmentWrite)]
    [ProducesResponseType(typeof(EquipmentItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EquipmentItemDto>> CreateAsync(
        [FromBody] CreateEquipmentItemRequest request,
        CancellationToken cancellationToken)
    {
        Result<EquipmentItemDto> result = await itemService.CreateAsync(request, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EquipmentWrite)]
    [ProducesResponseType(typeof(EquipmentItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentItemDto>> UpdateAsync(
        Guid id, [FromBody] UpdateEquipmentItemRequest request,
        CancellationToken cancellationToken)
    {
        Result<EquipmentItemDto> result = await itemService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.EquipmentDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await itemService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return NoContent();
    }
}