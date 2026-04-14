using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <inheritdoc />
public sealed class DocumentRepository(AppDbContext dbContext)
    : BaseRepository<Document>(dbContext), IDocumentRepository
{
    public async Task<Document?> GetWithDetailsAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.DocumentType)
            .Include(d => d.Category)
            .Include(d => d.Uploader)
            .Include(d => d.TargetDepartment)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Document?> GetForUpdateAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Document?> GetCurrentWithSignaturesAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Signatures)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByDocumentTypeAsync(
        Guid documentTypeId, Guid? documentTypeCategoryId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = DbSet
            .Include(d => d.DocumentType)
            .Include(d => d.Category)
            .Include(d => d.Uploader)
            .Where(d => d.DocumentTypeId == documentTypeId && d.IsActive);

        if (documentTypeCategoryId.HasValue)
            query = query.Where(d => d.DocumentTypeCategoryId == documentTypeCategoryId.Value);

        return await query
            .OrderByDescending(d => d.UploadedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetPendingForUserAsync(
        Guid userId,
        Guid? departmentId,
        Guid? jobTitleId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.DocumentType)
            .Include(d => d.Category)
            .Where(d => d.IsActive)
            .Where(d =>
                (d.TargetDepartmentId == null && d.TargetJobTitleId == null) ||
                (d.TargetDepartmentId != null && d.TargetDepartmentId == departmentId) ||
                (d.TargetJobTitleId != null && d.TargetJobTitleId == jobTitleId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await DbSet
            .Include(d => d.DocumentType)
            .Include(d => d.Category)
            .Where(d => idList.Contains(d.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentVersion> AddVersionAsync(
        DocumentVersion version, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<DocumentVersion>().AddAsync(version, cancellationToken);
        return version;
    }

    public Task SoftDeleteAsync(
        Document document, CancellationToken cancellationToken = default)
    {
        document.IsActive = false;
        document.DeletedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}