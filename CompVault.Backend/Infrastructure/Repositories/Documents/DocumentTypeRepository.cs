using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <inheritdoc />
public sealed class DocumentTypeRepository(AppDbContext dbContext)
    : BaseRepository<DocumentType>(dbContext), IDocumentTypeRepository
{
    public async Task<DocumentType?> GetBySlugAsync(
        string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dt => dt.Categories)
            .FirstOrDefaultAsync(dt => dt.Slug == slug, cancellationToken);
    }

    public async Task<DocumentType?> GetWithCategoriesBySlugAsync(
        string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dt => dt.Categories.Where(c => c.IsActive))
            .FirstOrDefaultAsync(dt => dt.Slug == slug, cancellationToken);
    }

    public async Task<DocumentType?> GetWithCategoriesAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(dt => dt.Categories)
            .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(
        string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            dt => dt.Slug == slug && (!excludeId.HasValue || dt.Id != excludeId.Value),
            cancellationToken);
    }
}