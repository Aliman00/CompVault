using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <inheritdoc />
public sealed class DocumentSignatureRepository(AppDbContext dbContext)
    : BaseRepository<DocumentSignature>(dbContext), IDocumentSignatureRepository
{
    public async Task<bool> HasUserSignedVersionAsync(
        Guid documentId, Guid userId, int version, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            s => s.DocumentId == documentId && s.UserId == userId && s.SignatureVersion == version,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentSignature>> GetForDocumentVersionAsync(
        Guid documentId, int version, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.User)
            .Where(s => s.DocumentId == documentId && s.SignatureVersion == version)
            .OrderByDescending(s => s.SignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentSignature>> GetByDocumentIdsAsync(
        IEnumerable<Guid> documentIds, CancellationToken cancellationToken = default)
    {
        var idList = documentIds.ToList();
        return await DbSet
            .Where(s => idList.Contains(s.DocumentId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetSignedDocumentIdsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.UserId == userId)
            .Select(s => s.DocumentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountForCurrentVersionAsync(
        Guid documentId, int currentVersion, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(
            s => s.DocumentId == documentId && s.SignatureVersion == currentVersion,
            cancellationToken);
    }

    public async Task DeleteAllForDocumentAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        List<DocumentSignature> signatures = await DbSet
            .Where(s => s.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        DbSet.RemoveRange(signatures);
    }
}