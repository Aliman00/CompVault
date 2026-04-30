using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Documents.Services;

public interface ISignatureService
{
    /// <summary>
    /// Henter alle signaturer for et dokument
    /// </summary>
    Task<Result<List<UserSignatureStatusDto>>> GetSignaturesAsync(string documentTypeSlug, Guid documentId, 
        CancellationToken ct);

    /// <summary>
    /// Signerer et dokument som innlogget bruker
    /// </summary>
    Task<Result> SignAsync(string documentTypeSlug, Guid documentId, CancellationToken ct);

    /// <summary>
    /// Henter alle dokumenter innlogget bruker har signert
    /// </summary>
    Task<Result<List<DocumentListDto>>> GetMySignedAsync(CancellationToken ct);

    /// <summary>
    /// Henter alle dokumenter innlogget bruker mangler å signere
    /// </summary>
    Task<Result<List<DocumentListDto>>> GetMyPendingAsync(CancellationToken ct);
}