using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Equipment.Services;
using CompVault.Backend.Infrastructure.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
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
    [ProducesResponseType(typeof(PagedResult<EquipmentIssuanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EquipmentIssuanceDto>>> GetAllAsync(
        [FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        Result<PagedResult<EquipmentIssuanceDto>> result =
            await issuanceService.GetAllAsync(query, cancellationToken);

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
    [ProducesResponseType(typeof(PagedResult<EquipmentIssuanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EquipmentIssuanceDto>>> GetByUserAsync(
        Guid userId, [FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        Result<PagedResult<EquipmentIssuanceDto>> result =
            await issuanceService.GetByUserAsync(userId, query, cancellationToken);

        if (result.IsFailure) return HandleFailure(result);
        return Ok(result.Value);
    }

    /// <summary>
    /// Henter alle utleveringer til en EquipmentItem
    /// </summary>
    [HttpGet("by-item/{itemId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<EquipmentIssuanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EquipmentIssuanceDto>>> GetByItemAsync(Guid itemId,
        CancellationToken ct)
    {
        Result<IReadOnlyList<EquipmentIssuanceDto>> result = await issuanceService.GetByItemAsync(itemId, ct);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>
    /// Henter utstyrskategorier med antall utstyr for innlogget bruker
    /// </summary>
    [HttpGet("my/categories")]
    [ProducesResponseType(typeof(IReadOnlyList<UserEquipmentCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserEquipmentCategoryDto>>> GetMyCategoriesAsync(
        CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        Result<IReadOnlyList<UserEquipmentCategoryDto>> result =
            await issuanceService.GetCategoriesForUserAsync(userId, ct);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>
    /// Henter utleveringer med filteringer og paginering for innlogget bruker
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<EquipmentIssuanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EquipmentIssuanceDto>>> GetMyEquipmentAsync(
        [FromQuery] Guid? categoryId, [FromQuery] PagedQuery query, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        Result<PagedResult<EquipmentIssuanceDto>> result =
            await issuanceService.GetMyEquipmentAsync(userId, categoryId, query, ct);

        if (result.IsFailure)
            return HandleFailure(result);

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
        Guid issuedById = User.GetUserId();
        Result<EquipmentIssuanceDto> result = await issuanceService.CreateAsync(issuedById, request, cancellationToken);

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