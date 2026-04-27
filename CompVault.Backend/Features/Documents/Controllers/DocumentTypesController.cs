using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Documents.Services;
using CompVault.Backend.Infrastructure.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Documents.Controllers;

/// <summary>
/// Administrasjon av dokumenttyper — opprett, oppdater, slett og kategorier.
/// </summary>
[ApiController]
[Route("api/document-types")]
[Authorize(Policy = Permissions.DocumentTypesRead)]
[Produces("application/json")]
public sealed class DocumentTypesController(IDocumentTypeService documentTypeService) : BaseController
{
    /// <summary>Henter alle dokumenttyper.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentTypeDto>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<DocumentTypeDto>> result = await documentTypeService.GetAllAsync(cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter én dokumenttype basert på slug.</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentTypeDto>> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        Result<DocumentTypeDto> result = await documentTypeService.GetBySlugAsync(slug, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
    
    /// <summary>Henter dokumenttyper med antall dokumenter for innlogget bruker.</summary>
    [HttpGet("my")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDocumentTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDocumentTypeDto>>> GetMyDocumentTypesAsync(CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        Result<IReadOnlyList<UserDocumentTypeDto>> result = 
            await documentTypeService.GetDocumentTypesForUserAsync(userId, ct);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny dokumenttype.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.DocumentTypesWrite)]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentTypeDto>> CreateAsync(
        [FromBody] CreateDocumentTypeRequest request, CancellationToken cancellationToken)
    {
        Guid createdById = User.GetUserId();

        Result<DocumentTypeDto> result = await documentTypeService.CreateAsync(
            request, createdById, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Created($"api/document-types/{result.Value!.Slug}", result.Value);
    }

    /// <summary>Oppdaterer en dokumenttype.</summary>
    [HttpPut("{slug}")]
    [Authorize(Policy = Permissions.DocumentTypesWrite)]
    [ProducesResponseType(typeof(DocumentTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentTypeDto>> UpdateAsync(
        string slug, [FromBody] UpdateDocumentTypeRequest request, CancellationToken cancellationToken)
    {
        Result<DocumentTypeDto> result = await documentTypeService.UpdateAsync(slug, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Soft-sletter en dokumenttype.</summary>
    [HttpDelete("{slug}")]
    [Authorize(Policy = Permissions.DocumentTypesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(string slug, CancellationToken cancellationToken)
    {
        Result<bool> result = await documentTypeService.DeleteAsync(slug, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }

    /// <summary>Henter kategorier for en dokumenttype.</summary>
    [HttpGet("{documentTypeSlug}/categories")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentTypeCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DocumentTypeCategoryDto>>> GetCategoriesAsync(
        string documentTypeSlug, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<DocumentTypeCategoryDto>> result =
            await documentTypeService.GetCategoriesAsync(documentTypeSlug, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter en ny kategori under en dokumenttype.</summary>
    [HttpPost("{documentTypeSlug}/categories")]
    [Authorize(Policy = Permissions.DocumentTypesWrite)]
    [ProducesResponseType(typeof(DocumentTypeCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentTypeCategoryDto>> CreateCategoryAsync(
        string documentTypeSlug,
        [FromBody] CreateDocumentTypeCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<DocumentTypeCategoryDto> result = await documentTypeService.CreateCategoryAsync(
            documentTypeSlug, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Created($"api/document-types/{documentTypeSlug}/categories", result.Value);
    }

    /// <summary>Oppdaterer en kategori.</summary>
    [HttpPut("{documentTypeSlug}/categories/{categoryId:guid}")]
    [Authorize(Policy = Permissions.DocumentTypesWrite)]
    [ProducesResponseType(typeof(DocumentTypeCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentTypeCategoryDto>> UpdateCategoryAsync(
        string documentTypeSlug, Guid categoryId,
        [FromBody] UpdateDocumentTypeCategoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<DocumentTypeCategoryDto> result = await documentTypeService.UpdateCategoryAsync(
            documentTypeSlug, categoryId, request, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Sletter en kategori.</summary>
    [HttpDelete("{documentTypeSlug}/categories/{categoryId:guid}")]
    [Authorize(Policy = Permissions.DocumentTypesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategoryAsync(
        string documentTypeSlug, Guid categoryId, CancellationToken cancellationToken)
    {
        Result<bool> result = await documentTypeService.DeleteCategoryAsync(
            documentTypeSlug, categoryId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }
}