using CompVault.Frontend.Common.Models;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public interface IDocumentService
{
    /// <summary>
    /// Henter alle dokumenter for en dokumenttype-slug
    /// </summary>
    Task<Result<List<DocumentListDto>>> GetAllAsync(string documentTypeSlug, CancellationToken ct);

    /// <summary>
    /// Henter et dokument fra backend. Krever slug og ID
    /// </summary>
    Task<Result<DocumentDto>> GetByIdAsync(string documentTypeSlug, Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter et nytt dokument med eventuelt vedlagt fil
    /// </summary>
    /// <param name="documentTypeSlug">Slug</param>
    /// <param name="request">CreateDocumentRequest</param>
    /// <param name="file">Fil som en FileAttachment</param>
    /// <param name="ct"></param>
    /// <returns>DocumentDto ved vellykket oppdatering</returns>
    Task<Result<DocumentDto>> CreateAsync(string documentTypeSlug, CreateDocumentRequest request,
        FileAttachment? file, CancellationToken ct);

    /// <summary>
    /// Oppdaterer eksisterende dokument. Krever Slug og ID
    /// </summary>
    Task<Result<DocumentDto>> UpdateAsync(string documentTypeSlug, Guid id, UpdateDocumentRequest request,
        CancellationToken ct);
    
    /// <summary>
    /// Oppdaterer et eksisterende dokument sin versjon
    /// </summary>
    /// <param name="documentTypeSlug">Slug</param>
    /// <param name="documentId">ID-en til dokumentet</param>
    /// <param name="file">Fil som FileAttachment - må stemme med eksisterende fil</param>
    /// <param name="ct"></param>
    /// <returns>Oppdatert DocumentDto</returns>
    Task<Result<DocumentDto>> UpdateVersionAsync(string documentTypeSlug, Guid documentId,
        FileAttachment? file, CancellationToken ct);

    /// <summary>
    /// Sletter et dokument
    /// </summary>
    Task<Result> DeleteAsync(string documentTypeSlug, Guid id, CancellationToken ct);
}