using CompVault.Backend.Domain.Entities.Competencies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Competencies;

/// <summary>
/// EF Core-konfigurasjon for kompetansebevis-tabellen.
/// Definerer relasjoner til ApplicationUser og CompetencyType, indekser for filtrering,
/// og konvertering av CompetencyStatus-enum til string for lesbarhet i databasen.
/// </summary>
internal sealed class CompetencyConfiguration : IEntityTypeConfiguration<Competency>
{
    public void Configure(EntityTypeBuilder<Competency> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.IssuedDate).IsRequired();
        builder.Property(c => c.CertificateNumber).HasMaxLength(100);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);

        // Indeks på Status — brukt av bakgrunnsjobb og filtrering
        builder.HasIndex(c => c.Status);

        // Compound index for vanlige spørringer: hent alle bevis for bruker X av type Y
        builder.HasIndex(c => new { c.UserId, c.CompetencyTypeId });

        // Indeks på ExpiryDate — brukt av bakgrunnsjobb
        builder.HasIndex(c => c.ExpiryDate);

        builder.HasIndex(c => c.DeletedAt);

        builder.HasQueryFilter(c => c.DeletedAt == null);

        // Relasjon: Competency → ApplicationUser (Many-to-One)
        builder.HasOne(c => c.ApplicationUser)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relasjon: Competency → CompetencyType (Many-to-One)
        builder.HasOne(c => c.CompetencyType)
            .WithMany(ct => ct.Competencies)
            .HasForeignKey(c => c.CompetencyTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}