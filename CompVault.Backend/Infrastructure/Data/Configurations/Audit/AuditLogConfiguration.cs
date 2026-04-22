using CompVault.Backend.Domain.Entities.Audit;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Audit;

/// <summary>
/// EF Core-konfigurasjon for AuditLog-tabellen.
/// Ingen FK til ApplicationUser — AuditLog er uavhengig av soft-delete.
/// Ingen query-filter — revisjonsloggen er alltid tilgjengelig.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.EntityId)
            .IsRequired();

        builder.Property(a => a.UserEmail)
            .HasMaxLength(256);

        builder.Property(a => a.UserName)
            .HasMaxLength(200);

        // JSONB for fleksible detaljer per action-type
        builder.Property(a => a.Details)
            .HasColumnType("jsonb");

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Indekser for vanlige spørringer
        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.CreatedAt).IsDescending();
    }
}