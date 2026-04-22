using CompVault.Backend.Domain.Entities.Equipment;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Equipment;

/// <summary>
/// EF Core-konfigurasjon for utstyrs-tabellen.
/// </summary>
internal sealed class EquipmentItemConfiguration : IEntityTypeConfiguration<EquipmentItem>
{
    public void Configure(EntityTypeBuilder<EquipmentItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.HasSize).IsRequired().HasDefaultValue(false);
        builder.Property(i => i.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(i => i.CreatedAt).IsRequired();

        // Indeks for spørringer: hent alle utstyr i en kategori + unikt navn per kategori
        builder.HasIndex(i => i.CategoryId);
        builder.HasIndex(i => new { i.CategoryId, i.Name }).IsUnique().HasFilter("\"DeletedAt\" IS NULL");
        builder.HasIndex(i => i.DeletedAt);

        builder.HasQueryFilter(i => i.DeletedAt == null);

        // Relasjon: Item → Category (Many-to-One)
        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}