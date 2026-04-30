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
}