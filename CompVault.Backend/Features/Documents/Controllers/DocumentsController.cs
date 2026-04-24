using CompVault.Backend.Common.Controller;
using CompVault.Backend.Features.Documents.Services;
using CompVault.Backend.Infrastructure.Extensions;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Features.Documents.Controllers;

/// <summary>
/// Generisk dokumentkontroller. Henter dokumenter basert på dokumenttype-slug.
/// </summary>
[ApiController]
[Route("api/documents/{documentTypeSlug}")]
[Produces("application/json")]
public sealed class DocumentsController(
    IDocumentService documentService,
    IDocumentVersioningService versioningService,
    IDocumentSignatureService signatureService) : BaseController
{
    /// <summary>Henter alle dokumenter for en dokumenttype.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentListDto>>> GetAllAsync(
        string documentTypeSlug,
        [FromQuery] Guid? documentTypeCategoryId,
        CancellationToken cancellationToken = default)
    {
        Guid? currentUserId = User.GetUserId();
        bool bypassTargeting = User.HasPermission(Permissions.DocumentsWrite);
        Result<IReadOnlyList<DocumentListDto>> result = await documentService.GetAllAsync(
            documentTypeSlug, currentUserId, documentTypeCategoryId, bypassTargeting, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter ett dokument.</summary>
    [HttpGet("{id:guid}", Name = "GetDocumentById")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Guid? currentUserId = User.GetUserId();
        bool bypassTargeting = User.HasPermission(Permissions.DocumentsWrite);
        Result<DocumentDto> result = await documentService.GetByIdAsync(id, currentUserId, bypassTargeting, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Oppretter et nytt dokument med valgfri filopplasting.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.DocumentsWrite)]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> CreateAsync(
        string documentTypeSlug,
        [FromForm] CreateDocumentRequest request,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        bool bypassTargeting = User.HasPermission(Permissions.DocumentsAllDepartments);

        if (file is not null && file.Length == 0)
            return BadRequest("Filen er tom.");

        Guid uploadedById = User.GetUserId();

        await using Stream? fileStream = file is not null ? file.OpenReadStream() : null;

        Result<DocumentDto> result = await documentService.CreateAsync(
            documentTypeSlug,
            request,
            uploadedById,
            bypassTargeting,
            file?.FileName,
            file?.ContentType,
            fileStream,
            cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtRoute("GetDocumentById",
            new { documentTypeSlug, id = result.Value!.Id }, result.Value);
    }

    /// <summary>Oppdaterer metadata på et dokument.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentsWrite)]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> UpdateAsync(
        Guid id,
        [FromBody] UpdateDocumentRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();
        bool bypassTargeting = User.HasPermission(Permissions.DocumentsAllDepartments);
        Result<DocumentDto> result = await documentService.UpdateAsync(id, userId, request, bypassTargeting,
            cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Soft-sletter et dokument.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Result<bool> result = await documentService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }

    /// <summary>Laster opp en ny filversjon.</summary>
    [HttpPost("{id:guid}/upload")]
    [Authorize(Policy = Permissions.DocumentsWrite)]
    [RequestSizeLimit(100 * 1024 * 1024)]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> UploadAsync(
        string documentTypeSlug, Guid id, IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Ingen fil lastet opp.");

        Guid uploadedById = User.GetUserId();

        await using Stream stream = file.OpenReadStream();
        Result<DocumentDto> result = await versioningService.UploadVersionAsync(
            id, documentTypeSlug, file.FileName, file.ContentType, stream, uploadedById, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtRoute("GetDocumentById",
            new { documentTypeSlug, id = result.Value!.Id }, result.Value);
    }

    /// <summary>Signerer et dokument.</summary>
    [HttpPost("{id:guid}/sign")]
    [Authorize(Policy = Permissions.DocumentsSign)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();

        Result<bool> result = await signatureService.SignAsync(id, userId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return NoContent();
    }

    /// <summary>Laster ned filen for et dokument.</summary>
    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Guid? currentUserId = User.GetUserId();
        bool bypassTargeting = User.HasPermission(Permissions.DocumentsWrite);
        Result<DocumentDownloadResult> result = await versioningService.GetDownloadAsync(
            id, currentUserId, bypassTargeting, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        DocumentDownloadResult download = result.Value!;

        // Åpne stream her i controlleren slik at ASP.NET Core kan håndtere disposal
        Stream fileStream = await versioningService.OpenFileStreamAsync(
            download.FilePath, cancellationToken);

        return File(fileStream, download.ContentType, download.FileName);
    }

    /// <summary>Henter signaturer for et dokument.</summary>
    [HttpGet("{id:guid}/signatures")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<UserSignatureStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<UserSignatureStatusDto>>> GetSignaturesAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Guid? currentUserId = User.GetUserId();
        bool bypassTargeting = User.HasPermission(Permissions.DocumentsWrite);
        Result<IReadOnlyList<UserSignatureStatusDto>> result = await signatureService.GetSignatureStatusAsync(
            id, currentUserId, bypassTargeting, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter fremdriftsstatistikk for en dokumenttype for innlogget bruker.</summary>
    [HttpGet("progress")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(DocumentProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentProgressDto>> GetProgressAsync(
        string documentTypeSlug, CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();

        Result<DocumentProgressDto> result = await signatureService.GetProgressAsync(
            documentTypeSlug, userId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter paginerte dokumenter brukeren har signert.</summary>
    [HttpGet("/api/documents/my/signed")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(PagedResult<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentListDto>>> GetMySignedDocumentsAsync(
        [FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();

        Result<PagedResult<DocumentListDto>> result = await signatureService.GetMySignedDocumentsAsync(
            userId, query, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter alle dokumenter brukeren trenger å signere.</summary>
    [HttpGet("/api/documents/my/pending")]
    [Authorize(Policy = Permissions.DocumentsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentListDto>>> GetMyPendingDocumentsAsync(
        CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();

        Result<IReadOnlyList<DocumentListDto>> result = await signatureService.GetMyPendingDocumentsAsync(
            userId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
    
    /// <summary>
    /// Henter alle dokumenter for en bruker, både signerte og ikke-signerte
    /// </summary>
    [HttpGet("/api/documents/user")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentListDto>>> GetMyDocumentsAsync(
        [FromQuery] DocumentQueryParameters query, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        bool hasPermission = User.HasPermission(Permissions.DocumentsRead);

        // Targeting skal aldri bypasses for dette endepunktet — vi henter kun dokumenter
        // brukeren selv er i målgruppen for, uavhengig av admin-tillatelser.
        Result<PagedResult<DocumentListDto>> result =
            await documentService.GetDocumentsForUserAsync(userId, query, hasPermission, ct);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
    
}