using CompVault.Backend.Domain.Entities.Equipment;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Equipment;

/// <summary>
/// EF Core-konfigurasjon for utstyrskategori-tabellen.
/// </summary>
internal sealed class EquipmentCategoryConfiguration : IEntityTypeConfiguration<EquipmentCategory>
{
    public void Configure(EntityTypeBuilder<EquipmentCategory> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(300);
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.Name).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(c => c.DeletedAt);
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}