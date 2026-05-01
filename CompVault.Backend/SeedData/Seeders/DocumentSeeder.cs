using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class DocumentSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        ApplicationUser? admin = await dbContext.Users
            .IgnoreQueryFilters()
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();
        if (admin is null)
        {
            logger.LogWarning("[Seeder] Ingen admin funnet – dokumenter hoppes over.");
            return;
        }

        foreach ((string documentTypeSlug, string? categorySlug, string title, bool requiresSignature, string? targetDeptName, string? targetJobTitleName) in BarnehageData.Documents)
        {
            DocumentType? documentType = await dbContext.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Slug == documentTypeSlug);
            if (documentType is null)
            {
                logger.LogWarning("[Seeder] Dokumenttype ikke funnet for dokument: {Slug}", documentTypeSlug);
                continue;
            }

            Guid? categoryId = null;
            if (categorySlug is not null)
            {
                DocumentTypeCategory? category = await dbContext.DocumentTypeCategories
                    .FirstOrDefaultAsync(c => c.DocumentTypeId == documentType.Id && c.Slug == categorySlug);
                categoryId = category?.Id;
            }

            Guid? targetDeptId = null;
            if (targetDeptName is not null)
            {
                Department? dept = await dbContext.Departments
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(d => d.Name == targetDeptName);
                targetDeptId = dept?.Id;
            }

            Guid? targetJobTitleId = null;
            if (targetJobTitleName is not null)
            {
                JobTitle? jobTitle = await dbContext.JobTitles
                    .FirstOrDefaultAsync(jt => jt.Name == targetJobTitleName);
                targetJobTitleId = jobTitle?.Id;
            }

            bool documentExists = await dbContext.Documents
                .IgnoreQueryFilters()
                .AnyAsync(d => d.Title == title && d.DocumentTypeId == documentType.Id);
            if (documentExists)
                continue;

            var documentDepartments = new List<DocumentDepartment>();
            if (targetDeptId.HasValue)
                documentDepartments.Add(new DocumentDepartment { DepartmentId = targetDeptId.Value });

            var documentJobTitles = new List<DocumentJobTitle>();
            if (targetJobTitleId.HasValue)
                documentJobTitles.Add(new DocumentJobTitle { JobTitleId = targetJobTitleId.Value });

            Document document = new()
            {
                DocumentTypeId = documentType.Id,
                DocumentTypeCategoryId = categoryId,
                Title = title,
                RequiresSignature = requiresSignature,
                DocumentDepartments = documentDepartments,
                DocumentJobTitles = documentJobTitles,
                Version = 1,
                IsActive = true,
                UploadedBy = admin.Id,
                UploadedAt = DateTime.UtcNow,
            };

            dbContext.Documents.Add(document);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] Dokument opprettet: {Title}", title);
        }
    }
}