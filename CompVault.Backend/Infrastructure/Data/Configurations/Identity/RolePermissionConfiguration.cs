using CompVault.Backend.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Identity;

/// <summary>
/// EF Core-konfigurasjon for koblingstabellen mellom roller og tillatelser.
/// Bruker sammensatt primærnøkkel (RoleId + PermissionId) — ingen duplikater.
/// </summary>
internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        // Sammensatt PK — samme tillatelse kan ikke tildeles samme rolle to ganger
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.Property(rp => rp.GrantedAt).IsRequired();

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
