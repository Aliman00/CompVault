using CompVault.Backend.Domain.Entities.Documents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Documents;

/// <summary>
/// EF Core-konfigurasjon for DocumentSignature-tabellen.
/// Unik constraint: én signatur per (DocumentId, UserId, SignatureVersion).
/// </summary>
internal sealed class DocumentSignatureConfiguration : IEntityTypeConfiguration<DocumentSignature>
{
    public void Configure(EntityTypeBuilder<DocumentSignature> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SignedAt).IsRequired();
        builder.Property(s => s.SignatureVersion).IsRequired();

        // Unik constraint: én signatur per bruker per versjon per dokument
        builder.HasIndex(s => new { s.DocumentId, s.UserId, s.SignatureVersion }).IsUnique();

        builder.HasIndex(s => s.UserId);

        // Matcher Document's query filter slik at navigasjon til Document alltid fungerer
        builder.HasQueryFilter(s => s.Document == null || s.Document.DeletedAt == null);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}