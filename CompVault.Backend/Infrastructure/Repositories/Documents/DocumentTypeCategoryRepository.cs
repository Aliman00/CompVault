using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <inheritdoc />
public sealed class DocumentTypeCategoryRepository(AppDbContext dbContext)
    : BaseRepository<DocumentTypeCategory>(dbContext), IDocumentTypeCategoryRepository
{
    public async Task<IReadOnlyList<DocumentTypeCategory>> GetByDocumentTypeIdAsync(
        Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(c => c.DocumentTypeId == documentTypeId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(
        Guid documentTypeId, string slug, Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            c => c.DocumentTypeId == documentTypeId && c.Slug == slug
                 && (!excludeId.HasValue || c.Id != excludeId.Value),
            cancellationToken);
    }
}