using CompVault.Backend.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Identity;

/// <summary>
/// EF Core-konfigurasjon for tillatelsestabellen.
/// Name er unik — vi vil ikke ha duplikater som "users:read" to ganger.
/// </summary>
internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        // Unik indeks så vi ikke ved et uhell oppretter samme tillatelse to ganger
        builder.HasIndex(p => p.Name).IsUnique();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Category).HasMaxLength(100);
    }
}
