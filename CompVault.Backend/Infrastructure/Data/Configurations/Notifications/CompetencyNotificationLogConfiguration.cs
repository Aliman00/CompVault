using CompVault.Backend.Domain.Entities.Notifications;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Notifications;

/// <summary>
/// EF Core-konfigurasjon for varslingslogg-tabellen.
/// Definerer FK til Competency, unik constraint for deduplisering,
/// og indekser for effektive oppslag.
/// </summary>
internal sealed class CompetencyNotificationLogConfiguration : IEntityTypeConfiguration<CompetencyNotificationLog>
{
    public void Configure(EntityTypeBuilder<CompetencyNotificationLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.RecipientEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(l => l.RecipientRole)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.ThresholdDays).IsRequired();
        builder.Property(l => l.SentAt).IsRequired();

        // Unik constraint — forhindrer at samme varsel sendes til samme person to ganger
        builder.HasIndex(l => new { l.CompetencyId, l.ThresholdDays, l.RecipientEmail })
            .IsUnique();

        // Indeks for effektive oppslag under varslingskjøringen
        builder.HasIndex(l => new { l.CompetencyId, l.ThresholdDays });

        // Relasjon: CompetencyNotificationLog → Competency
        // IsRequired(false) forhindrer query-filter-konflikt med Competency sitt soft-delete filter.
        // Varslingsloggen er en audit-trail og skal bestå selv om kompetansen filtreres bort.
        builder.HasOne(l => l.Competency)
            .WithMany()
            .HasForeignKey(l => l.CompetencyId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
