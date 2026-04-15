using CompVault.Backend.Domain.Entities.Documents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Documents;

/// <summary>
/// EF Core-konfigurasjon for DocumentTypeCategory-tabellen.
/// </summary>
internal sealed class DocumentTypeCategoryConfiguration : IEntityTypeConfiguration<DocumentTypeCategory>
{
    public void Configure(EntityTypeBuilder<DocumentTypeCategory> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(50).IsRequired();
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(c => new { c.DocumentTypeId, c.Slug })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Kategorien er usynlig hvis den selv er soft-slettet ELLER hvis foreldretypen er soft-slettet
        builder.HasQueryFilter(c => c.DeletedAt == null && (c.DocumentType == null || c.DocumentType.DeletedAt == null));

        builder.HasMany<Document>()
            .WithOne(d => d.Category)
            .HasForeignKey(d => d.DocumentTypeCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}