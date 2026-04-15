using CompVault.Backend.Domain.Entities.Documents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Documents;

/// <summary>
/// EF Core-konfigurasjon for DocumentType-tabellen.
/// </summary>
internal sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.HasKey(dt => dt.Id);

        builder.Property(dt => dt.Name).HasMaxLength(100).IsRequired();
        builder.Property(dt => dt.Slug).HasMaxLength(50).IsRequired();
        builder.Property(dt => dt.Description).HasMaxLength(500);
        builder.Property(dt => dt.TargetMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(dt => dt.StorageFolder).HasMaxLength(100).IsRequired();
        builder.Property(dt => dt.MaxFileSizeBytes).IsRequired();
        builder.Property(dt => dt.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(dt => dt.CreatedAt).IsRequired();

        builder.HasOne(dt => dt.CreatedBy)
            .WithMany()
            .HasForeignKey(dt => dt.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(dt => dt.Slug).IsUnique();
        builder.HasIndex(dt => dt.DeletedAt);

        builder.HasQueryFilter(dt => dt.DeletedAt == null);

        builder.HasMany(dt => dt.Categories)
            .WithOne(c => c.DocumentType)
            .HasForeignKey(c => c.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<Document>()
            .WithOne(d => d.DocumentType)
            .HasForeignKey(d => d.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}