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
/// Generisk dokumentkontroller. Henter dokumenter basert på dokumenttype-slug.
/// </summary>
[ApiController]
[Route("api/documents/{documentTypeSlug}")]
[Authorize(Policy = Permissions.DocumentsRead)]
[Produces("application/json")]
public sealed class DocumentsController(
    IDocumentService documentService,
    IDocumentTypeService documentTypeService) : BaseController
{
    /// <summary>Henter alle dokumenter for en dokumenttype.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentListDto>>> GetAllAsync(
        string documentTypeSlug,
        [FromQuery] Guid? documentTypeCategoryId,
        CancellationToken cancellationToken = default)
    {
        Guid? currentUserId = User.TryGetUserId();
        Result<IReadOnlyList<DocumentListDto>> result = await documentService.GetAllAsync(
            documentTypeSlug, currentUserId, documentTypeCategoryId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter ett dokument.</summary>
    [HttpGet("{id:guid}", Name = "GetDocumentById")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        Result<DocumentDto> result = await documentService.GetByIdAsync(id, cancellationToken);

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
        if (file is not null)
        {
            if (file.Length == 0)
                return BadRequest("Filen er tom.");

            // Hent dokumenttype for å validere mot typespesifikke grenser
            Result<Shared.DTOs.Documents.DocumentTypeDto> typeResult =
                await documentTypeService.GetBySlugAsync(documentTypeSlug, cancellationToken);

            if (typeResult.IsFailure)
                return HandleFailure(typeResult);

            Shared.DTOs.Documents.DocumentTypeDto docType = typeResult.Value!;

            if (file.Length > docType.MaxFileSizeBytes)
                return BadRequest($"Filen er for stor. Maks tillatt størrelse for denne dokumenttypen: {docType.MaxFileSizeBytes / (1024 * 1024)}MB.");

            if (!docType.AllowedMimeTypes.Contains(file.ContentType))
                return BadRequest($"Filtypen '{file.ContentType}' er ikke tillatt for denne dokumenttypen.");
        }

        Guid uploadedById = User.TryGetUserId()
            ?? throw new InvalidOperationException("Kunne ikke hente bruker-ID fra token.");

        await using Stream? fileStream = file is not null ? file.OpenReadStream() : null;

        Result<DocumentDto> result = await documentService.CreateAsync(
            documentTypeSlug,
            request,
            uploadedById,
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
        Result<DocumentDto> result = await documentService.UpdateAsync(id, request, cancellationToken);

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

        // Hent dokumenttype for å validere mot typespesifikke grenser
        Result<Shared.DTOs.Documents.DocumentTypeDto> typeResult =
            await documentTypeService.GetBySlugAsync(documentTypeSlug, cancellationToken);

        if (typeResult.IsFailure)
            return HandleFailure(typeResult);

        Shared.DTOs.Documents.DocumentTypeDto docType = typeResult.Value!;

        if (file.Length > docType.MaxFileSizeBytes)
            return BadRequest($"Filen er for stor. Maks tillatt størrelse for denne dokumenttypen: {docType.MaxFileSizeBytes / (1024 * 1024)}MB.");

        if (!docType.AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest($"Filtypen '{file.ContentType}' er ikke tillatt for denne dokumenttypen.");

        Guid uploadedById = User.TryGetUserId()
            ?? throw new InvalidOperationException("Kunne ikke hente bruker-ID fra token.");

        await using Stream stream = file.OpenReadStream();
        Result<DocumentDto> result = await documentService.UploadVersionAsync(
            id, documentTypeSlug, file.FileName, file.ContentType, stream, uploadedById, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return CreatedAtRoute("GetDocumentById",
            new { documentTypeSlug, id = result.Value!.Id }, result.Value);
    }

    /// <summary>Signerer et dokument.</summary>
    [HttpPost("{id:guid}/sign")]
    [Authorize(Policy = Permissions.DocumentsSign)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignAsync(
        string documentTypeSlug, Guid id, CancellationToken cancellationToken)
    {
        Guid userId = User.TryGetUserId()
            ?? throw new InvalidOperationException("Kunne ikke hente bruker-ID fra token.");

        Result<bool> result = await documentService.SignAsync(id, userId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Created(new Uri($"/api/documents/{documentTypeSlug}/{id}", UriKind.Relative), null);
    }

    /// <summary>Laster ned filen for et dokument.</summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Result<DocumentDownloadResult> result = await documentService.GetDownloadAsync(id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        DocumentDownloadResult download = result.Value!;

        // Åpne stream her i controlleren slik at ASP.NET Core kan håndtere disposal
        Stream fileStream = await documentService.OpenFileStreamAsync(
            download.FilePath, cancellationToken);

        return File(fileStream, download.ContentType, download.FileName);
    }

    /// <summary>Henter signaturer for et dokument.</summary>
    [HttpGet("{id:guid}/signatures")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentSignatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DocumentSignatureDto>>> GetSignaturesAsync(
        Guid id, CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<DocumentSignatureDto>> result = await documentService.GetSignaturesAsync(
            id, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter alle dokumenter brukeren har signert.</summary>
    [HttpGet("/api/documents/my/signed")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentListDto>>> GetMySignedDocumentsAsync(
        CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();

        Result<IReadOnlyList<DocumentListDto>> result = await documentService.GetMySignedDocumentsAsync(
            userId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }

    /// <summary>Henter alle dokumenter brukeren trenger å signere.</summary>
    [HttpGet("/api/documents/my/pending")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DocumentListDto>>> GetMyPendingDocumentsAsync(
        CancellationToken cancellationToken)
    {
        Guid userId = User.GetUserId();

        Result<IReadOnlyList<DocumentListDto>> result = await documentService.GetMyPendingDocumentsAsync(
            userId, cancellationToken);

        if (result.IsFailure)
            return HandleFailure(result);

        return Ok(result.Value);
    }
}