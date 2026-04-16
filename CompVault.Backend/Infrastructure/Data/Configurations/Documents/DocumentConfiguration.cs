using CompVault.Backend.Domain.Entities.Documents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Documents;

/// <summary>
/// EF Core-konfigurasjon for Document-tabellen.
/// </summary>
internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(2000);
        builder.Property(d => d.ExternalUrl).HasMaxLength(500);
        builder.Property(d => d.FileName).HasMaxLength(255);
        builder.Property(d => d.FilePath).HasMaxLength(500);
        builder.Property(d => d.MimeType).HasMaxLength(100);
        builder.Property(d => d.Checksum).HasMaxLength(64);
        builder.Property(d => d.Version).IsRequired();
        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(d => d.UploadedAt).IsRequired();

        // Indeks for filtrering på dokumenttype og aktiv status
        builder.HasIndex(d => new { d.DocumentTypeId, d.IsActive });

        builder.HasIndex(d => d.DeletedAt);

        // Indeks for DocumentTypeCategoryId
        builder.HasIndex(d => d.DocumentTypeCategoryId);

        builder.HasQueryFilter(d => d.DeletedAt == null);

        // Relasjon: Document → DocumentTypeCategory (Many-to-One, optional)
        builder.HasOne(d => d.Category)
            .WithMany()
            .HasForeignKey(d => d.DocumentTypeCategoryId);

        // Relasjon: Document → ApplicationUser (uploader)
        builder.HasOne(d => d.Uploader)
            .WithMany()
            .HasForeignKey(d => d.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Relasjon: Document → DocumentVersion (One-to-Many)
        builder.HasMany(d => d.Versions)
            .WithOne(v => v.Document)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relasjon: Document → DocumentSignature (One-to-Many)
        builder.HasMany(d => d.Signatures)
            .WithOne(s => s.Document)
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Målgruppe (many-to-many via koblingstabeller) konfigureres i
        // DocumentDepartmentConfiguration og DocumentJobTitleConfiguration.
    }
}