using CompVault.Backend.Domain.Entities.Documents;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <summary>
/// Repository for dokumentsignaturer.
/// </summary>
public interface IDocumentSignatureRepository : IRepository<DocumentSignature>
{
    /// <summary>Sjekker om en bruker har signert en gitt versjon.</summary>
    Task<bool> HasUserSignedVersionAsync(
        Guid documentId, Guid userId, int version, CancellationToken cancellationToken = default);

    /// <summary>Henter signaturer for en bestemt versjon av et dokument.</summary>
    Task<IReadOnlyList<DocumentSignature>> GetForDocumentVersionAsync(
        Guid documentId, int version, CancellationToken cancellationToken = default);

    /// <summary>Henter alle signaturer for en liste med dokument-IDer.</summary>
    Task<IReadOnlyList<DocumentSignature>> GetByDocumentIdsAsync(
        IEnumerable<Guid> documentIds, CancellationToken cancellationToken = default);

    /// <summary>Henter alle dokument-IDer en bruker har signert.</summary>
    Task<IReadOnlyList<Guid>> GetSignedDocumentIdsAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Henter alle signaturer for et dokument (for tracked delete).</summary>
    Task<IReadOnlyList<DocumentSignature>> GetForDocumentAsync(
        Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Markerer en signatur for sletting i change-trackeren. Kall SaveChangesAsync for å persistere.</summary>
    void Remove(DocumentSignature signature);
}