using CompVault.Backend.Domain.Entities.Equipment;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Equipment;

/// <summary>
/// EF Core-konfigurasjon for utleverings-tabellen.
/// Definerer relasjoner til ApplicationUser (to FK-er) og EquipmentItem.
/// </summary>
internal sealed class EquipmentIssuanceConfiguration : IEntityTypeConfiguration<EquipmentIssuance>
{
    public void Configure(EntityTypeBuilder<EquipmentIssuance> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).IsRequired().HasDefaultValue(1);
        builder.Property(i => i.Size).HasMaxLength(20);
        builder.Property(i => i.IssuedDate).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(i => i.CreatedAt).IsRequired();

        // Indeks for spørringer: hent alle utleveringer for en bruker
        builder.HasIndex(i => i.UserId);
        // Indeks for spørringer: hent alle utleveringer av et utstyr
        builder.HasIndex(i => i.ItemId);
        builder.HasIndex(i => i.DeletedAt);

        builder.HasQueryFilter(i => i.DeletedAt == null);
        
        // Relasjon: Issuance → ApplicationUser (UserId — hvem fikk utstyret)
        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Relasjon: Issuance → ApplicationUser (IssuedById — hvem delte ut)
        builder.HasOne(i => i.IssuedBy)
            .WithMany()
            .HasForeignKey(i => i.IssuedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}