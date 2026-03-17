using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Hoved-DbContext for appen. Arver Identity sin DbContext så vi får
/// alle Identity-tabellene gratis, pluss våre egne domenetabeller.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    // ============= IDENTITY ==============
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ============= AUTH ==============
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();



    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Plukker automatisk opp alle IEntityTypeConfiguration-klasser i assembly-et.
        // Ingen grunn til å registrere dem manuelt når du legger til nye entiteter.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
