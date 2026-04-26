using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Enums;
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
            .Include(d => d.DocumentDepartments).ThenInclude(dd => dd.Department)
            .Include(d => d.DocumentJobTitles).ThenInclude(dj => dj.JobTitle)
            .Include(d => d.Uploader)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Document?> GetForUpdateAsync(
        Guid id, CancellationToken cancellationToken = default) => await DbSet
            .IgnoreQueryFilters()
            .Include(d => d.DocumentType)
            .Include(d => d.DocumentDepartments).ThenInclude(dd => dd.Department)
            .Include(d => d.DocumentJobTitles).ThenInclude(dj => dj.JobTitle)
            .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null, cancellationToken);
    
    public async Task<Document?> GetCurrentWithSignaturesAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Signatures)
            .Include(d => d.DocumentDepartments).ThenInclude(dd => dd.Department)
            .Include(d => d.DocumentJobTitles).ThenInclude(dj => dj.JobTitle)
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
            .Include(d => d.DocumentDepartments).ThenInclude(dd => dd.Department)
            .Include(d => d.DocumentJobTitles).ThenInclude(dj => dj.JobTitle)
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
        return await ApplyTargetingFilter(
                DbSet
                    .Include(d => d.DocumentType)
                    .Include(d => d.Category)
                    .Include(d => d.DocumentDepartments).ThenInclude(dd => dd.Department)
                    .Include(d => d.DocumentJobTitles).ThenInclude(dj => dj.JobTitle)
                    .Include(d => d.Uploader)
                    .Where(d => d.IsActive),
                departmentId, jobTitleId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetAccessibleByDocumentTypeAsync(
        Guid documentTypeId,
        Guid? departmentId,
        Guid? jobTitleId,
        CancellationToken cancellationToken = default)
    {
        return await ApplyTargetingFilter(
                DbSet.Where(d => d.DocumentTypeId == documentTypeId && d.IsActive),
                departmentId, jobTitleId)
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
            .Include(d => d.DocumentDepartments).ThenInclude(dd => dd.Department)
            .Include(d => d.DocumentJobTitles).ThenInclude(dj => dj.JobTitle)
            .Include(d => d.Uploader)
            .Where(d => idList.Contains(d.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<UserDocumentTypeDto>> GetDocumentTypesForUserAsync(
        Guid userId, Guid? departmentId, Guid? jobTitleId, CancellationToken ct = default) => 
        await ApplyTargetingFilter(DbSet.Where(d => d.IsActive), departmentId, jobTitleId)
            .GroupBy(d => new {  // Henter ut det vi trenger for DTO-en
                d.DocumentTypeId, 
                d.DocumentType!.Name,
                d.DocumentType.Slug,
                d.DocumentType.Description })
            .Select(g => new UserDocumentTypeDto
            {
                Id = g.Key.DocumentTypeId,
                Name = g.Key.Name,
                Slug = g.Key.Slug,
                Description = g.Key.Description,
                DocumentCount = g.Count() // Teller antall dokumenter til hver type
            })
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    
    
    public async Task<int> CountDocumentsForUserAsync(
        Guid userId,
        Guid? departmentId,
        Guid? jobTitleId,
        DocumentQueryParameters parameters,
        CancellationToken ct = default)
    {
        IQueryable<Document> query = ApplyTargetingFilter(DbSet.Where(d => d.IsActive),
            departmentId, jobTitleId);

        query = ApplySignatureFilter(query, userId, parameters.SignatureFilter);
        
        if (parameters.DocumentTypeSlug is not null)
            query = query.Where(d => d.DocumentType!.Slug == parameters.DocumentTypeSlug);

        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetDocumentsForUserAsync(
        Guid userId,
        Guid? departmentId,
        Guid? jobTitleId,
        DocumentQueryParameters parameters,
        CancellationToken ct = default)
    {      
        // Henter alle hvis vi har tilattelse
        IQueryable<Document> query = ApplyTargetingFilter(DbSet.Where(d => d.IsActive), 
            departmentId, jobTitleId);
        
        // Filterer bort utifra om vi har valgt alle, signatuerer eller ikke signaturer
        query = ApplySignatureFilter(query, userId, parameters.SignatureFilter);
        
        // Filtrerer etter dokumenttype
        if (parameters.DocumentTypeSlug is not null)
            query = query.Where(d => d.DocumentType!.Slug == parameters.DocumentTypeSlug);

        IOrderedQueryable<Document> sorted = parameters.SortBy switch
        {
            DocumentSortField.Title => parameters.SortDescending
                ? query.OrderByDescending(d => d.Title)
                : query.OrderBy(d => d.Title),
            DocumentSortField.Version => parameters.SortDescending
                ? query.OrderByDescending(d => d.Version)
                : query.OrderBy(d => d.Version),
            _ => parameters.SortDescending
                ? query.OrderByDescending(d => d.UploadedAt)
                : query.OrderBy(d => d.UploadedAt)
        };

        return await sorted
            .Include(d => d.DocumentType) 
            .Include(d => d.Category)
            .Include(d => d.DocumentDepartments).ThenInclude(dd => dd.Department)
            .Include(d => d.DocumentJobTitles).ThenInclude(dj => dj.JobTitle)
            .Include(d => d.Uploader)
            .Skip(parameters.Skip)
            .Take(parameters.PageSize)
            .AsNoTracking()
            .ToListAsync(ct);
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

    /// <summary>
    /// Filtrerer dokumenter basert på målgruppe (avdeling/jobbtittel).
    /// Dokumenter uten målgruppe er synlige for alle.
    /// Når både avdeling og jobbtittel er satt, kreves match på begge (AND-logikk).
    /// </summary>
    private static IQueryable<Document> ApplyTargetingFilter(IQueryable<Document> query, Guid? departmentId,
        Guid? jobTitleId)
    {
        return query.Where(d =>
            (!d.DocumentDepartments.Any() && !d.DocumentJobTitles.Any()) ||
            (d.DocumentDepartments.Any() && !d.DocumentJobTitles.Any() &&
             d.DocumentDepartments.Any(dd => dd.DepartmentId == departmentId)) ||
            (!d.DocumentDepartments.Any() && d.DocumentJobTitles.Any() &&
             d.DocumentJobTitles.Any(dj => dj.JobTitleId == jobTitleId)) ||
            (d.DocumentDepartments.Any() && d.DocumentJobTitles.Any() &&
             d.DocumentDepartments.Any(dd => dd.DepartmentId == departmentId) &&
             d.DocumentJobTitles.Any(dj => dj.JobTitleId == jobTitleId)));
    }
    
    /// <summary>
    /// Vi filtrerer dokumenter utifra om vi ønsker å hente signerte, ikke-signerte eller alle.
    /// Pending: dokumentet krever signatur, og brukeren har ikke signert gjeldende versjon.
    /// Signed: brukeren har signert gjeldende versjon.
    /// </summary>
    private IQueryable<Document> ApplySignatureFilter(
        IQueryable<Document> query,
        Guid userId,
        DocumentSignatureFilter signatureFilter) => signatureFilter switch
    {
        DocumentSignatureFilter.Signed => query
            .Where(d => DbContext.Set<DocumentSignature>()
                .Any(s => s.DocumentId == d.Id && s.UserId == userId
                          && s.SignatureVersion == d.Version)),
        DocumentSignatureFilter.Pending => query
            .Where(d =>
                d.RequiresSignature &&
                !DbContext.Set<DocumentSignature>()
                    .Any(s => s.DocumentId == d.Id && s.UserId == userId
                              && s.SignatureVersion == d.Version)),
        _ => query
    };
}