using CompVault.Backend.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Identity;

/// <summary>
/// EF Core-konfigurasjon for brukertabellen.
/// Setter kolonnetyper, lengdebegrensninger og relasjoner til leder og avdeling.
/// HasQueryFilter sørger for at soft-slettede brukere aldri dukker opp i spørringer.
/// </summary>
internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.JobTitle).HasMaxLength(150);
        // Lagrer enum som streng i DB så det er lesbart uten å slå opp i kode
        builder.Property(u => u.EmploymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.CreatedAt).IsRequired();
        // Global filter — soft-slettede og inaktive brukere filtreres bort automatisk overalt
        builder.HasQueryFilter(u => u.DeletedAt == null && u.IsActive);

        // Selvrefererende relasjon: en bruker kan ha en leder som også er en bruker
        builder.HasOne(u => u.Manager)
            .WithMany(u => u.DirectReports)
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Department)
            .WithMany(d => d.Members)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Selvrefererende relasjon: hvem opprettet denne brukeren
        builder.HasOne(u => u.CreatedBy)
            .WithMany()
            .HasForeignKey(u => u.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
