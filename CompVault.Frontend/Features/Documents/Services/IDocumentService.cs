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
    /// Henter et dokument fra backend
    /// </summary>
    Task<Result<DocumentDto>> GetByIdAsync(string documentTypeSlug, Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter et nytt dokument med eventuelt vedlagt fil
    /// </summary>
    Task<Result<DocumentDto>> CreateAsync(string documentTypeSlug, CreateDocumentRequest request,
        FileAttachment? file, CancellationToken ct);

    /// <summary>
    /// Oppdaterer eksisterende dokument
    /// </summary>
    Task<Result<DocumentDto>> UpdateAsync(string documentTypeSlug, Guid id, UpdateDocumentRequest request,
        CancellationToken ct);

    /// <summary>
    /// Sletter et dokument
    /// </summary>
    Task<Result> DeleteAsync(string documentTypeSlug, Guid id, CancellationToken ct);
}