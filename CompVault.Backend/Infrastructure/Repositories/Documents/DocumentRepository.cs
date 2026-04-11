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
            .FirstOrDefaultAsync(d => d.Id == id && d.IsCurrent, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByDocumentTypeAsync(
        Guid documentTypeId, Guid? documentTypeCategoryId, bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Document> query = DbSet
            .Include(d => d.DocumentType)
            .Include(d => d.Category)
            .Include(d => d.Uploader)
            .Where(d => d.DocumentTypeId == documentTypeId && d.IsActive);

        if (!includeArchived)
            query = query.Where(d => d.IsCurrent);

        if (documentTypeCategoryId.HasValue)
            query = query.Where(d => d.DocumentTypeCategoryId == documentTypeCategoryId.Value);

        return await query
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetActiveCurrentForDepartmentAsync(
        Guid departmentId, Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.DocumentTypeId == documentTypeId
                        && d.IsActive
                        && d.IsCurrent
                        && (d.TargetDepartmentId == null || d.TargetDepartmentId == departmentId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetActiveCurrentForJobTitleAsync(
        string jobTitle, Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.DocumentTypeId == documentTypeId
                        && d.IsActive
                        && d.IsCurrent
                        && (d.TargetJobTitle == null || d.TargetJobTitle == jobTitle))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetAllActiveCurrentAsync(
        Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(d => d.DocumentTypeId == documentTypeId
                        && d.IsActive
                        && d.IsCurrent)
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
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentVersion> AddVersionAsync(
        DocumentVersion version, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<DocumentVersion>().AddAsync(version, cancellationToken);
        return version;
    }

    public async Task SoftDeleteAsync(
        Document document, CancellationToken cancellationToken = default)
    {
        document.IsActive = false;
        document.DeletedAt = DateTime.UtcNow;
        DbSet.Update(document);
    }
}