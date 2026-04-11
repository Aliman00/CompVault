using CompVault.Backend.Domain.Entities.Documents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Documents;

/// <summary>
/// EF Core-konfigurasjon for DocumentVersion-tabellen.
/// </summary>
internal sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Version).IsRequired();
        builder.Property(v => v.FileName).HasMaxLength(255);
        builder.Property(v => v.FilePath).HasMaxLength(500);
        builder.Property(v => v.MimeType).HasMaxLength(100);
        builder.Property(v => v.Checksum).HasMaxLength(64);
        builder.Property(v => v.ArchivedAt).IsRequired();

        builder.HasIndex(v => new { v.DocumentId, v.Version });

        // Matcher Document's query filter slik at navigasjon til Document alltid fungerer
        builder.HasQueryFilter(v => v.Document == null || v.Document.DeletedAt == null);
    }
}