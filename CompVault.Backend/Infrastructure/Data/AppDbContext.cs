using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Domain.Entities.Notifications;
using CompVault.Backend.Features.Departments.Services;
using Perms = CompVault.Shared.Constants.Permissions;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Hoved-DbContext for appen. Arver Identity sin DbContext så vi får
/// alle Identity-tabellene gratis, pluss våre egne domenetabeller.
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDepartmentScopeService scope) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    // ============= IDENTITY ==============
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ============= COMPETENCIES ==============
    public DbSet<CompetencyType> CompetencyTypes => Set<CompetencyType>();
    public DbSet<Competency> Competencies => Set<Competency>();

    // ============= DOCUMENTS ==============
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentTypeCategory> DocumentTypeCategories => Set<DocumentTypeCategory>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentSignature> DocumentSignatures => Set<DocumentSignature>();
    public DbSet<DocumentDepartment> DocumentDepartments => Set<DocumentDepartment>();
    public DbSet<DocumentJobTitle> DocumentJobTitles => Set<DocumentJobTitle>();

    // ============= AUTH ==============
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ============= EQUIPMENT ==============
    public DbSet<EquipmentCategory> EquipmentCategories => Set<EquipmentCategory>();
    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();
    public DbSet<EquipmentIssuance> EquipmentIssuances => Set<EquipmentIssuance>();

    // ============= AUDIT ==============
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // ============= NOTIFICATIONS ==============
    public DbSet<CompetencyNotificationLog> CompetencyNotificationLogs => Set<CompetencyNotificationLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Plukker automatisk opp alle IEntityTypeConfiguration-klasser i assembly-et.
        // Ingen grunn til å registrere dem manuelt når du legger til nye entiteter.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Legger til query-filter på hver bruker
        builder.Entity<ApplicationUser>().HasQueryFilter(u =>
            u.DeletedAt == null &&
            (scope.HasBypass(Perms.UsersAll) ||
             scope.GetAllowedDepartmentIds(Perms.UsersReadSub).Contains(u.DepartmentId)));
    }
}