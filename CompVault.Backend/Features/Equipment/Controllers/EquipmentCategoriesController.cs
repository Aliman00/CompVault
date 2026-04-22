using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Equipment.Services;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Equipment.Controllers;

/// <summary>
/// Administrasjon av utstyrskategorier.
/// </summary>
[ApiController]
[Route(ApiRoutes.EquipmentCategories.Base)]
[Authorize(Policy = Permissions.EquipmentRead)]
[Produces("application/json")]
public sealed class EquipmentCategoriesController(IEquipmentCategoryService categoryService) : BaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EquipmentCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentCategoryDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EquipmentCategoryDto>> result =
            await categoryService.GetAllAsync(cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EquipmentCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentCategoryDto>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Result<EquipmentCategoryDto> result = await categoryService.GetByIdAsync(id, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.EquipmentWrite)]
    [ProducesResponseType(typeof(EquipmentCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EquipmentCategoryDto>> CreateAsync(
        [FromBody] CreateEquipmentCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<EquipmentCategoryDto> result = await categoryService.CreateAsync(request, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return CreatedAtAction("GetById", new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.EquipmentWrite)]
    [ProducesResponseType(typeof(EquipmentCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentCategoryDto>> UpdateAsync(
        Guid id, [FromBody] UpdateEquipmentCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<EquipmentCategoryDto> result = await categoryService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.EquipmentDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await categoryService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return NoContent();
    }
}